using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_range_t" /> struct.</summary>
public static unsafe partial class cef_range_tTests
{
    /// <summary>Validates that the <see cref="cef_range_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_range_t), Marshal.SizeOf<cef_range_t>());
    }

    /// <summary>Validates that the <see cref="cef_range_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_range_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_range_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        Assert.Equal(8, sizeof(cef_range_t));
    }
}
