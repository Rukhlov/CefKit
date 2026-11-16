using System;
using System.Runtime.InteropServices;
using Xunit;

namespace CefKit.Interop.UnitTests;

/// <summary>Provides validation of the <see cref="cef_dev_tools_message_observer_t" /> struct.</summary>
public static unsafe partial class cef_dev_tools_message_observer_tTests
{
    /// <summary>Validates that the <see cref="cef_dev_tools_message_observer_t" /> struct is blittable.</summary>
    [Fact]
    public static void IsBlittableTest()
    {
        Assert.Equal(sizeof(cef_dev_tools_message_observer_t), Marshal.SizeOf<cef_dev_tools_message_observer_t>());
    }

    /// <summary>Validates that the <see cref="cef_dev_tools_message_observer_t" /> struct has the right <see cref="LayoutKind" />.</summary>
    [Fact]
    public static void IsLayoutSequentialTest()
    {
        Assert.True(typeof(cef_dev_tools_message_observer_t).IsLayoutSequential);
    }

    /// <summary>Validates that the <see cref="cef_dev_tools_message_observer_t" /> struct has the correct size.</summary>
    [Fact]
    public static void SizeOfTest()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(80, sizeof(cef_dev_tools_message_observer_t));
        }
        else
        {
            Assert.Equal(40, sizeof(cef_dev_tools_message_observer_t));
        }
    }
}
