namespace CefKit.Interop;

public unsafe partial struct cef_media_access_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_media_access_callback_t *, uint32_t) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_media_access_callback_t*, uint, void> cont;

    [NativeTypeName("void (*)(struct _cef_media_access_callback_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_media_access_callback_t*, void> cancel;
}
