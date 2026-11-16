using CefKit.Interop;
using Xunit;

namespace CefKit.Tests;

/// <summary>
/// Reference counting of the structs that .NET objects give to CEF, driven through the struct's own function
/// pointers as CEF would call them.
/// </summary>
public unsafe class CefHandlerTests
{
    [Fact]
    public void StructLivesWhileCefHoldsReferences()
    {
        var task = new CountingTask();
        var native = (cef_task_t*)task.AddRefNative();
        var b = &native->@base;

        Assert.Equal((nuint)sizeof(cef_task_t), b->size);
        Assert.Equal(1, task.NativeRefCount);
        Assert.Equal(1, b->has_one_ref(b));

        b->add_ref(b);
        Assert.Equal(2, task.NativeRefCount);
        Assert.Equal(0, b->has_one_ref(b));
        Assert.Equal(1, b->has_at_least_one_ref(b));

        Assert.Equal(0, b->release(b));
        Assert.Equal(1, task.NativeRefCount);

        // The last release frees the struct: the next hand-over allocates a new one
        Assert.Equal(1, b->release(b));
        Assert.Equal(0, task.NativeRefCount);
    }

    [Fact]
    public void CallbacksReachTheDotNetObject()
    {
        var task = new CountingTask();
        var native = (cef_task_t*)task.AddRefNative();
        native->execute(native);
        native->execute(native);
        Assert.Equal(2, task.Runs);
        Assert.Equal(1, native->@base.release(&native->@base));
    }

    [Fact]
    public void HandOverAfterReleaseAllocatesAgain()
    {
        var task = new CountingTask();
        var first = (cef_task_t*)task.AddRefNative();
        Assert.Equal(1, first->@base.release(&first->@base));

        var second = (cef_task_t*)task.AddRefNative();
        Assert.Equal(1, task.NativeRefCount);
        second->execute(second);
        Assert.Equal(1, task.Runs);
        Assert.Equal(1, second->@base.release(&second->@base));
    }

    [Fact]
    public void ConcurrentAddRefAndReleaseKeepCount()
    {
        var task = new CountingTask();
        var native = (cef_task_t*)task.AddRefNative();
        var b = (nint)(&native->@base);

        Parallel.For(0, 10_000, _ =>
        {
            var p = (cef_base_ref_counted_t*)b;
            p->add_ref(p);
            p->release(p);
        });

        Assert.Equal(1, task.NativeRefCount);
        Assert.Equal(1, native->@base.release(&native->@base));
    }

    /// <summary>
    /// A CEF task that counts its runs.
    /// </summary>
    private sealed class CountingTask : CefHandler<cef_task_t>
    {
        public int Runs { get; private set; }

        private protected override void Fill(cef_task_t* native) => native->execute = &Execute;

        [System.Runtime.InteropServices.UnmanagedCallersOnly]
        private static void Execute(cef_task_t* self) => FromNative<CountingTask>(self).Runs++;
    }
}
