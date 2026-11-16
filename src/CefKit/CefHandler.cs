using System.Runtime.InteropServices;
using CefKit.Interop;

namespace CefKit;

/// <summary>
/// Base of the CEF interfaces implemented in .NET: handlers, tasks, observers. CEF sees a native struct of
/// function pointers with a reference count. The struct exists while CEF holds references to it and keeps
/// the .NET object alive until CEF releases the last one; the next hand-over to CEF allocates a new struct.
/// </summary>
public abstract unsafe class CefHandler
{
    private readonly Lock _lock = new();
    private byte* _native;

    private protected CefHandler()
    {
    }

    /// <summary>
    /// Size of the CEF struct, the value of its base.size field.
    /// </summary>
    private protected abstract int StructSize { get; }

    /// <summary>
    /// Sets the function pointers of the interface in a zeroed struct. A null pointer means CEF's default behavior.
    /// </summary>
    private protected abstract void FillCallbacks(void* native);

    /// <summary>
    /// The native struct with a new reference for the receiver: an argument that CEF takes ownership of, or a
    /// value returned to CEF.
    /// </summary>
    internal void* AddRefNative()
    {
        lock (_lock)
        {
            if (_native == null)
            {
                var size = StructSize;
                var native = (byte*)NativeMemory.AllocZeroed((nuint)(size + sizeof(Trailer)));
                var b = (cef_base_ref_counted_t*)native;
                b->size = (nuint)size;
                b->add_ref = &AddRef;
                b->release = &Release;
                b->has_one_ref = &HasOneRef;
                b->has_at_least_one_ref = &HasAtLeastOneRef;
                FillCallbacks(native);
                ((Trailer*)(native + size))->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                _native = native;
            }

            Interlocked.Increment(ref GetTrailer(_native)->RefCount);
            return _native;
        }
    }

    /// <summary>
    /// The .NET object behind the struct that CEF passed as self.
    /// </summary>
    internal static T FromNative<T>(void* self)
        where T : CefHandler =>
        (T)GCHandle.FromIntPtr(GetTrailer(self)->Handle).Target!;

    /// <summary>
    /// Number of references CEF holds (for tests and diagnostics).
    /// </summary>
    internal int NativeRefCount
    {
        get
        {
            lock (_lock)
            {
                return _native == null ? 0 : Volatile.Read(ref GetTrailer(_native)->RefCount);
            }
        }
    }

    /// <summary>
    /// The trailer lives right after the CEF struct, whose size is in its first field.
    /// </summary>
    private static Trailer* GetTrailer(void* self) => (Trailer*)((byte*)self + ((cef_base_ref_counted_t*)self)->size);

    [UnmanagedCallersOnly]
    private static void AddRef(cef_base_ref_counted_t* self) => Interlocked.Increment(ref GetTrailer(self)->RefCount);

    [UnmanagedCallersOnly]
    private static int Release(cef_base_ref_counted_t* self)
    {
        // Take the owner while the reference still keeps the struct alive
        var trailer = GetTrailer(self);
        var owner = (CefHandler)GCHandle.FromIntPtr(trailer->Handle).Target!;
        if (Interlocked.Decrement(ref trailer->RefCount) != 0)
        {
            return 0;
        }

        owner.FreeUnreferenced((byte*)self);
        return 1;
    }

    [UnmanagedCallersOnly]
    private static int HasOneRef(cef_base_ref_counted_t* self) => Volatile.Read(ref GetTrailer(self)->RefCount) == 1 ? 1 : 0;

    [UnmanagedCallersOnly]
    private static int HasAtLeastOneRef(cef_base_ref_counted_t* self) => Volatile.Read(ref GetTrailer(self)->RefCount) > 0 ? 1 : 0;

    /// <summary>
    /// Frees the struct after CEF released the last reference, unless a new reference was handed out meanwhile.
    /// </summary>
    private void FreeUnreferenced(byte* native)
    {
        lock (_lock)
        {
            if (_native != native)
            {
                return;
            }

            var trailer = GetTrailer(native);
            if (Volatile.Read(ref trailer->RefCount) != 0)
            {
                return;
            }

            GCHandle.FromIntPtr(trailer->Handle).Free();
            _native = null;
            NativeMemory.Free(native);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Trailer
    {
        public nint Handle;
        public int RefCount;
    }
}

/// <summary>
/// A CEF interface implemented in .NET, typed by its CEF struct.
/// </summary>
public abstract unsafe class CefHandler<TStruct> : CefHandler
    where TStruct : unmanaged
{
    private protected CefHandler()
    {
    }

    private protected sealed override int StructSize => sizeof(TStruct);

    private protected sealed override void FillCallbacks(void* native) => Fill((TStruct*)native);

    /// <summary>
    /// Sets the function pointers that this class implements.
    /// </summary>
    private protected abstract void Fill(TStruct* native);
}
