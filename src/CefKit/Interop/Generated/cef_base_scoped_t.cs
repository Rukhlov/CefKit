namespace CefKit.Interop;

public unsafe partial struct cef_base_scoped_t
{
    [NativeTypeName("size_t")]
    public nuint size;

    [NativeTypeName("void (*)(struct _cef_base_scoped_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_base_scoped_t*, void> del;
}
