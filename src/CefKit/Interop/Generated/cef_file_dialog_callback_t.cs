namespace CefKit.Interop;

public unsafe partial struct cef_file_dialog_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_file_dialog_callback_t *, cef_string_list_t) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_file_dialog_callback_t*, cef_string_list_t*, void> cont;

    [NativeTypeName("void (*)(struct _cef_file_dialog_callback_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_file_dialog_callback_t*, void> cancel;
}
