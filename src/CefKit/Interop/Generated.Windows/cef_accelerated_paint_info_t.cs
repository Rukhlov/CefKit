using CefKit.Interop;

namespace CefKit.Interop.Windows;

public partial struct cef_accelerated_paint_info_t
{
    [NativeTypeName("size_t")]
    public nuint size;

    [NativeTypeName("cef_shared_texture_handle_t")]
    public nint shared_texture_handle;

    public cef_color_type_t format;

    public cef_accelerated_paint_info_common_t extra;
}
