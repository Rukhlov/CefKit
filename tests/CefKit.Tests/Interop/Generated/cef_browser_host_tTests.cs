using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_browser_host_t" /> struct.</summary>
public static unsafe partial class cef_browser_host_tTests
{
    /// <summary>Validates that the <see cref="cef_browser_host_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_browser_host_t), Marshal.SizeOf<cef_browser_host_t>());
    }

    /// <summary>Validates that the <see cref="cef_browser_host_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_browser_host_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_browser_host_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(584, sizeof(cef_browser_host_t));
        }
        else
        {
            Assert.Equal(292, sizeof(cef_browser_host_t));
        }
    }
}
