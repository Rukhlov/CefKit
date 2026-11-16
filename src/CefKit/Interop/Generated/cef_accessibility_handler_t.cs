namespace CefKit.Interop;

public unsafe partial struct cef_accessibility_handler_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_accessibility_handler_t *, struct _cef_value_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_accessibility_handler_t*, cef_value_t*, void> on_accessibility_tree_change;

    [NativeTypeName("void (*)(struct _cef_accessibility_handler_t *, struct _cef_value_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_accessibility_handler_t*, cef_value_t*, void> on_accessibility_location_change;
}
