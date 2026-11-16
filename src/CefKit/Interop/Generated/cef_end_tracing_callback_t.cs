namespace CefKit.Interop;

public unsafe partial struct cef_end_tracing_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_end_tracing_callback_t *, const cef_string_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_end_tracing_callback_t*, cef_string_utf16_t*, void> on_end_tracing_complete;
}
