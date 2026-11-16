namespace CefKit.Interop;

public unsafe partial struct cef_task_t
{
    public cef_base_ref_counted_t @base;

    [NativeTypeName("void (*)(struct _cef_task_t *) __attribute__((stdcall))")]
    public delegate* unmanaged<cef_task_t*, void> execute;
}
