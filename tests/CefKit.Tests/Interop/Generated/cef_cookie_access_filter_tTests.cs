using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_cookie_access_filter_t" /> struct.</summary>
public static unsafe partial class cef_cookie_access_filter_tTests
{
    /// <summary>Validates that the <see cref="cef_cookie_access_filter_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_cookie_access_filter_t), Marshal.SizeOf<cef_cookie_access_filter_t>());
    }

    /// <summary>Validates that the <see cref="cef_cookie_access_filter_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_cookie_access_filter_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_cookie_access_filter_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(56, sizeof(cef_cookie_access_filter_t));
        }
        else
        {
            Assert.Equal(28, sizeof(cef_cookie_access_filter_t));
        }
    }
}
