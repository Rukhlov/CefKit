using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.Linux.UnitTests;

/// <summary>Provides validation of the <see cref="cef_accelerated_paint_native_pixmap_plane_t" /> struct.</summary>
public static unsafe partial class cef_accelerated_paint_native_pixmap_plane_tTests
{
    /// <summary>Validates that the <see cref="cef_accelerated_paint_native_pixmap_plane_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_accelerated_paint_native_pixmap_plane_t), Marshal.SizeOf<cef_accelerated_paint_native_pixmap_plane_t>());
    }

    /// <summary>Validates that the <see cref="cef_accelerated_paint_native_pixmap_plane_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_accelerated_paint_native_pixmap_plane_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_accelerated_paint_native_pixmap_plane_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(32, sizeof(cef_accelerated_paint_native_pixmap_plane_t));
        }
        else
        {
            Assert.Equal(16, sizeof(cef_accelerated_paint_native_pixmap_plane_t));
        }
    }
}
