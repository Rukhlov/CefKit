namespace CefKit.Interop;

public unsafe partial struct cef_preference_registrar_t
{
    public cef_base_scoped_t @base;

    [NativeTypeName("int (*)(struct _cef_preference_registrar_t *, const cef_string_t *, struct _cef_value_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_preference_registrar_t*, cef_string_utf16_t*, cef_value_t*, int> add_preference;

    [NativeTypeName("int (*)(struct _cef_preference_registrar_t *, const cef_string_t *, struct _cef_value_t *, cef_string_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_preference_registrar_t*, cef_string_utf16_t*, cef_value_t*, cef_string_utf16_t*, int> set_default_preference;

    [NativeTypeName("struct _cef_value_t *(*)(struct _cef_preference_registrar_t *, const cef_string_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_preference_registrar_t*, cef_string_utf16_t*, cef_value_t*> get_default_preference;
}
