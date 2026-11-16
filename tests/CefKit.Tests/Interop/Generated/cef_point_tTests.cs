using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_point_t" /> struct.</summary>
public static unsafe partial class cef_point_tTests
{
    /// <summary>Validates that the <see cref="cef_point_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_point_t), Marshal.SizeOf<cef_point_t>());
    }

    /// <summary>Validates that the <see cref="cef_point_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_point_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_point_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        Assert.Equal(8, sizeof(cef_point_t));
    }
}
