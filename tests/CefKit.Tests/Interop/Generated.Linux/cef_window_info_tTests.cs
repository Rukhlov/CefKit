using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.Linux.UnitTests;

/// <summary>Provides validation of the <see cref="cef_window_info_t" /> struct.</summary>
public static unsafe partial class cef_window_info_tTests
{
    /// <summary>Validates that the <see cref="cef_window_info_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_window_info_t), Marshal.SizeOf<cef_window_info_t>());
    }

    /// <summary>Validates that the <see cref="cef_window_info_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_window_info_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_window_info_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(88, sizeof(cef_window_info_t));
        }
        else
        {
            Assert.Equal(56, sizeof(cef_window_info_t));
        }
    }
}
