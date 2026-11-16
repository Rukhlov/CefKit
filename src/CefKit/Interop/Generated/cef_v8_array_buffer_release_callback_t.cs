namespace CefKit.Interop;

public unsafe partial struct cef_v8_array_buffer_release_callback_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_v8_array_buffer_release_callback_t *, void *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_v8_array_buffer_release_callback_t*, void*, void> release_buffer;
}
