namespace CefKit.Interop;

public unsafe partial struct cef_media_route_create_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_media_route_create_callback_t *, cef_media_route_create_result_t, const cef_string_t *, struct _cef_media_route_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_media_route_create_callback_t*, cef_media_route_create_result_t, cef_string_utf16_t*, cef_media_route_t*, void> on_media_route_create_finished;
}
