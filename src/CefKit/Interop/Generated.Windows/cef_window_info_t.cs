using CefKit.Interop;

namespace CefKit.Interop.Windows;

public partial struct cef_window_info_t
{
    [NativeTypeName("size_t")]
    public nuint size;

    [NativeTypeName("DWORD")]
    public uint ex_style;

    [NativeTypeName("cef_string_t")]
    public cef_string_utf16_t window_name;

    [NativeTypeName("DWORD")]
    public uint style;

    public cef_rect_t bounds;

    [NativeTypeName("cef_window_handle_t")]
    public nint parent_window;

    [NativeTypeName("HMENU")]
    public nint menu;

    public int windowless_rendering_enabled;

    public int shared_texture_enabled;

    public int external_begin_frame_enabled;

    [NativeTypeName("cef_window_handle_t")]
    public nint window;

    public cef_runtime_style_t runtime_style;
}
