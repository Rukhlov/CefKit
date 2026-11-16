using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_insets_t" /> struct.</summary>
public static unsafe partial class cef_insets_tTests
{
    /// <summary>Validates that the <see cref="cef_insets_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_insets_t), Marshal.SizeOf<cef_insets_t>());
    }

    /// <summary>Validates that the <see cref="cef_insets_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_insets_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_insets_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        Assert.Equal(16, sizeof(cef_insets_t));
    }
}
