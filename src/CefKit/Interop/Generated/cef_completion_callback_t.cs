namespace CefKit.Interop;

public unsafe partial struct cef_completion_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_completion_callback_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_completion_callback_t*, void> on_complete;
}
