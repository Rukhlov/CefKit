namespace CefKit.Interop;

public unsafe partial struct cef_set_cookie_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_set_cookie_callback_t *, int) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_set_cookie_callback_t*, int, void> on_complete;
}
