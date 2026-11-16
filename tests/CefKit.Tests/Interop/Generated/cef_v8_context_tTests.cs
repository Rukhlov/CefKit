using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_v8_context_t" /> struct.</summary>
public static unsafe partial class cef_v8_context_tTests
{
    /// <summary>Validates that the <see cref="cef_v8_context_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_v8_context_t), Marshal.SizeOf<cef_v8_context_t>());
    }

    /// <summary>Validates that the <see cref="cef_v8_context_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_v8_context_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_v8_context_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(112, sizeof(cef_v8_context_t));
        }
        else
        {
            Assert.Equal(56, sizeof(cef_v8_context_t));
        }
    }
}
