using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Tests;

public sealed class CaptureRegionTests
{
    [Fact]
    public void ValidateWithin_AcceptsRegionInsideWindow()
    {
        CaptureRegion region = new(10, 20, 100, 120);

        region.ValidateWithin(200, 200);
    }

    [Theory]
    [InlineData(-1, 0, 100, 100)]
    [InlineData(0, -1, 100, 100)]
    [InlineData(0, 0, 0, 100)]
    [InlineData(0, 0, 100, 0)]
    public void ValidateWithin_RejectsInvalidCoordinatesOrSize(int x, int y, int width, int height)
    {
        CaptureRegion region = new(x, y, width, height);

        Assert.Throws<ArgumentOutOfRangeException>(() => region.ValidateWithin(200, 200));
    }

    [Theory]
    [InlineData(150, 0, 100, 100)]
    [InlineData(0, 150, 100, 100)]
    public void ValidateWithin_RejectsRegionOutsideWindow(int x, int y, int width, int height)
    {
        CaptureRegion region = new(x, y, width, height);

        Assert.Throws<ArgumentOutOfRangeException>(() => region.ValidateWithin(200, 200));
    }
}
