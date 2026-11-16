using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_v8_stack_frame_t" /> struct.</summary>
public static unsafe partial class cef_v8_stack_frame_tTests
{
    /// <summary>Validates that the <see cref="cef_v8_stack_frame_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_v8_stack_frame_t), Marshal.SizeOf<cef_v8_stack_frame_t>());
    }

    /// <summary>Validates that the <see cref="cef_v8_stack_frame_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_v8_stack_frame_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_v8_stack_frame_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(104, sizeof(cef_v8_stack_frame_t));
        }
        else
        {
            Assert.Equal(52, sizeof(cef_v8_stack_frame_t));
        }
    }
}
