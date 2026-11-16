using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_accelerated_paint_info_common_t" /> struct.</summary>
public static unsafe partial class cef_accelerated_paint_info_common_tTests
{
    /// <summary>Validates that the <see cref="cef_accelerated_paint_info_common_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_accelerated_paint_info_common_t), Marshal.SizeOf<cef_accelerated_paint_info_common_t>());
    }

    /// <summary>Validates that the <see cref="cef_accelerated_paint_info_common_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_accelerated_paint_info_common_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_accelerated_paint_info_common_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        Assert.Equal(112, sizeof(cef_accelerated_paint_info_common_t));
    }
}
