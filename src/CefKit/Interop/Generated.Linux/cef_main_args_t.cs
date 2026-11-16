using CefKit.Interop;

namespace CefKit.Interop.Linux;

public unsafe partial struct cef_main_args_t
{
    public int argc;

    [NativeTypeName("char **")]
    public sbyte** argv;
}
