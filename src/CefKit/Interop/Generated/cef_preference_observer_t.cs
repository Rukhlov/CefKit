namespace CefKit.Interop;

public unsafe partial struct cef_preference_observer_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_preference_observer_t *, const cef_string_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_preference_observer_t*, cef_string_utf16_t*, void> on_preference_changed;
}
