using CefKit.Interop;

namespace CefKit.Interop.Windows;

public partial struct cef_main_args_t
{
    [NativeTypeName("HINSTANCE")]
    public nint instance;
}
