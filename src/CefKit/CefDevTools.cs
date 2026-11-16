using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace CefKit;

/// <summary>
/// DevTools protocol calls for one browser from the browser process: JavaScript evaluation without code in the
/// render process. Create with <see cref="CreateAsync"/>, dispose to unregister.
/// </summary>
public sealed class CefDevToolsClient : IDisposable
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly CefBrowserHost _host;
    private readonly Observer _observer;
    private CefRegistration? _registration;
    private int _lastId;

    private CefDevToolsClient(CefBrowserHost host)
    {
        _host = host;
        _observer = new Observer(this);
    }

    /// <summary>
    /// Registers a DevTools observer on the browser; the client keeps its own reference to the browser host.
    /// </summary>
    public static async Task<CefDevToolsClient> CreateAsync(CefBrowser browser)
    {
        ArgumentNullException.ThrowIfNull(browser);
        var client = new CefDevToolsClient(browser.GetHost());
        try
        {
            client._registration = await CefRuntime.InvokeAsync(CefThreadId.UI, () => client._host.AddDevToolsMessageObserver(client._observer))
                ?? throw new InvalidOperationException("The browser did not accept a DevTools observer");
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Calls a DevTools method and returns its "result" dictionary. Fails with <see cref="CefDevToolsException"/>
    /// when the method returns an error.
    /// </summary>
    public async Task<JsonElement> ExecuteAsync(string method, Action<Utf8JsonWriter>? writeParams = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ObjectDisposedException.ThrowIf(_registration is null, this);
        var id = Interlocked.Increment(ref _lastId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        try
        {
            var message = BuildMessage(id, method, writeParams);
            var sent = await CefRuntime.InvokeAsync(CefThreadId.UI, () => _host.SendDevToolsMessage(message));
            if (!sent)
            {
                throw new CefDevToolsException($"{method} was not sent: the browser is closing");
            }

            return await completion.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Evaluates a JavaScript expression in the main frame and returns its value as JSON (Runtime.evaluate with
    /// returnByValue); with awaitPromise a promise is awaited. Script exceptions fail with <see cref="CefDevToolsException"/>.
    /// </summary>
    public async Task<JsonElement> EvaluateAsync(string expression, bool awaitPromise = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var result = await ExecuteAsync("Runtime.evaluate", writer =>
        {
            writer.WriteString("expression", expression);
            writer.WriteBoolean("returnByValue", true);
            writer.WriteBoolean("awaitPromise", awaitPromise);
        }, cancellationToken);

        if (result.TryGetProperty("exceptionDetails", out var exception))
        {
            var text = exception.TryGetProperty("exception", out var value) && value.TryGetProperty("description", out var description)
                ? description.GetString()
                : exception.TryGetProperty("text", out var plain) ? plain.GetString() : exception.GetRawText();
            throw new CefDevToolsException($"Script failed: {text}");
        }

        return result.TryGetProperty("result", out var remote) && remote.TryGetProperty("value", out var resultValue)
            ? resultValue
            : default;
    }

    /// <summary>
    /// Unregisters the observer and fails the calls in flight.
    /// </summary>
    public void Dispose()
    {
        var registration = Interlocked.Exchange(ref _registration, null);
        if (registration is not null && CefRuntime.IsInitialized)
        {
            // The registration belongs to the UI thread; releasing it there removes the observer
            if (!CefRuntime.PostTask(CefThreadId.UI, registration.Dispose))
            {
                registration.Dispose();
            }
        }

        foreach (var pending in _pending.Values)
        {
            pending.TrySetException(new ObjectDisposedException(nameof(CefDevToolsClient)));
        }

        _host.Dispose();
    }

    private static byte[] BuildMessage(int id, string method, Action<Utf8JsonWriter>? writeParams)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", id);
            writer.WriteString("method", method);
            writer.WriteStartObject("params");
            writeParams?.Invoke(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private void Complete(int messageId, bool success, ReadOnlySpan<byte> result)
    {
        if (!_pending.TryGetValue(messageId, out var completion))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(result.ToArray());
            var element = document.RootElement.Clone();
            if (success)
            {
                completion.TrySetResult(element);
            }
            else
            {
                var message = element.TryGetProperty("message", out var text) ? text.GetString() : Encoding.UTF8.GetString(result);
                completion.TrySetException(new CefDevToolsException(message ?? "DevTools error"));
            }
        }
        catch (JsonException ex)
        {
            completion.TrySetException(new CefDevToolsException($"Bad DevTools result: {ex.Message}"));
        }
    }

    private sealed class Observer(CefDevToolsClient client) : CefDevToolsMessageObserver
    {
        protected override void OnDevToolsMethodResult(CefBrowser browser, int messageId, bool success, ReadOnlySpan<byte> result) =>
            client.Complete(messageId, success, result);
    }
}

/// <summary>
/// A DevTools call failed.
/// </summary>
public sealed class CefDevToolsException(string message) : Exception(message);
