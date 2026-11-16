using CefKit.Interop;
using System.Runtime.CompilerServices;

namespace CefKit.Interop.Linux;

public partial struct cef_accelerated_paint_info_t
{
    [NativeTypeName("size_t")]
    public nuint size;

    [NativeTypeName("cef_accelerated_paint_native_pixmap_plane_t[4]")]
    public _planes_e__FixedBuffer planes;

    public int plane_count;

    [NativeTypeName("uint64_t")]
    public ulong modifier;

    public cef_color_type_t format;

    public cef_accelerated_paint_info_common_t extra;

    [InlineArray(4)]
    public partial struct _planes_e__FixedBuffer
    {
        public cef_accelerated_paint_native_pixmap_plane_t e0;
    }
}
