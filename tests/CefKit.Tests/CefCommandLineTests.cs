using Xunit;

namespace CefKit.Tests;

/// <summary>
/// Merging values into comma-separated list switches (disable-features...), the part of
/// <see cref="CefCommandLine.AppendToListSwitch"/> that runs without libcef.
/// </summary>
public class CefCommandLineTests
{
    [Fact]
    public void ValuesAreAddedToAnEmptyList()
    {
        Assert.Equal("A", CefCommandLine.MergeList(string.Empty, ["A"]));
    }

    [Fact]
    public void ExistingValuesStayFirstAndAreNotRepeated()
    {
        Assert.Equal("EnableHangWatcher,LensOverlay,CalculateNativeWinOcclusion",
            CefCommandLine.MergeList("EnableHangWatcher,LensOverlay", ["LensOverlay", "CalculateNativeWinOcclusion"]));
    }

    [Fact]
    public void NothingNewGivesNull()
    {
        Assert.Null(CefCommandLine.MergeList("A,B", ["B", "A", " "]));
    }

    [Fact]
    public void ValuesMayBeListsAndAreTrimmed()
    {
        Assert.Equal("A,B,C,D", CefCommandLine.MergeList("A, B,", ["C,D", " C "]));
    }
}
