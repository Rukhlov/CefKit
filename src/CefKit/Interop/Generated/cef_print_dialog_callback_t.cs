namespace CefKit.Interop;

public unsafe partial struct cef_print_dialog_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_print_dialog_callback_t *, struct _cef_print_settings_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_print_dialog_callback_t*, cef_print_settings_t*, void> cont;

    [NativeTypeName("void (*)(struct _cef_print_dialog_callback_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_print_dialog_callback_t*, void> cancel;
}
