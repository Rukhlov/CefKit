using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_request_context_settings_t" /> struct.</summary>
public static unsafe partial class cef_request_context_settings_tTests
{
    /// <summary>Validates that the <see cref="cef_request_context_settings_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_request_context_settings_t), Marshal.SizeOf<cef_request_context_settings_t>());
    }

    /// <summary>Validates that the <see cref="cef_request_context_settings_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_request_context_settings_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_request_context_settings_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(96, sizeof(cef_request_context_settings_t));
        }
        else
        {
            Assert.Equal(48, sizeof(cef_request_context_settings_t));
        }
    }
}
