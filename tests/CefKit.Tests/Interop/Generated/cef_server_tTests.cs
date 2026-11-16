using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_server_t" /> struct.</summary>
public static unsafe partial class cef_server_tTests
{
    /// <summary>Validates that the <see cref="cef_server_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_server_t), Marshal.SizeOf<cef_server_t>());
    }

    /// <summary>Validates that the <see cref="cef_server_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_server_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_server_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(144, sizeof(cef_server_t));
        }
        else
        {
            Assert.Equal(72, sizeof(cef_server_t));
        }
    }
}
