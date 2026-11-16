using CefKit.Interop;

namespace CefKit.Interop.Linux;

public partial struct cef_accelerated_paint_native_pixmap_plane_t
{
    [NativeTypeName("uint32_t")]
    public uint stride;

    [NativeTypeName("uint64_t")]
    public ulong offset;

    [NativeTypeName("uint64_t")]
    public ulong size;

    public int fd;
}
