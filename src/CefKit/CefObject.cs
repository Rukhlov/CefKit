using CefKit.Interop;

namespace CefKit;

/// <summary>
/// Base of the objects implemented by CEF (browser, frame, request context...). The .NET object owns one CEF
/// reference and releases it on <see cref="Dispose"/>. Objects passed to handler methods are valid during the
/// call only: CefKit releases them when the method returns, keep a <c>Clone()</c> to use one later.
/// </summary>
public abstract unsafe class CefObject : IDisposable
{
    private nint _ptr;

    /// <summary>
    /// Takes ownership of one reference.
    /// </summary>
    private protected CefObject(void* ptr)
    {
        if (ptr == null)
        {
            throw new ArgumentNullException(nameof(ptr));
        }

        _ptr = (nint)ptr;
    }

    /// <summary>
    /// The reference was released by <see cref="Dispose"/>.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _ptr) == 0;

    /// <summary>
    /// The native object; throws after <see cref="Dispose"/>.
    /// </summary>
    private protected void* NativePtr
    {
        get
        {
            var ptr = Volatile.Read(ref _ptr);
            ObjectDisposedException.ThrowIf(ptr == 0, this);
            return (void*)ptr;
        }
    }

    /// <summary>
    /// Releases the CEF reference.
    /// </summary>
    public void Dispose()
    {
        ReleaseNative();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// A new reference to the same native object for an argument that CEF takes ownership of.
    /// </summary>
    internal void* AddRefNative()
    {
        var b = (cef_base_ref_counted_t*)NativePtr;
        b->add_ref(b);
        return b;
    }

    /// <summary>
    /// Releases a forgotten reference while CEF runs; after shutdown CEF objects are gone.
    /// </summary>
    ~CefObject()
    {
        if (CefRuntime.IsInitialized)
        {
            ReleaseNative();
        }
    }

    private void ReleaseNative()
    {
        var b = (cef_base_ref_counted_t*)Interlocked.Exchange(ref _ptr, 0);
        if (b != null)
        {
            b->release(b);
        }
    }
}
