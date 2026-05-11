using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrRegionTests
{
    [Fact]
    public void Parse_ReadsXyWidthHeight()
    {
        OcrRegion region = OcrRegion.Parse("50,80,700,120");

        Assert.Equal(new OcrRegion(50, 80, 700, 120), region);
    }

    [Theory]
    [InlineData("1,2,3")]
    [InlineData("1,2,3,4,5")]
    [InlineData("1,two,3,4")]
    public void Parse_RejectsInvalidFormat(string value)
    {
        Assert.Throws<ArgumentException>(() => OcrRegion.Parse(value));
    }

    [Theory]
    [InlineData(-1, 0, 10, 10)]
    [InlineData(0, -1, 10, 10)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 0, 10, 0)]
    public void ValidateShape_RejectsInvalidCoordinatesOrSize(int x, int y, int width, int height)
    {
        OcrRegion region = new(x, y, width, height);

        Assert.Throws<ArgumentOutOfRangeException>(region.ValidateShape);
    }

    [Fact]
    public void ValidateWithin_AcceptsRegionInsideImage()
    {
        OcrRegion region = new(10, 20, 30, 40);

        region.ValidateWithin(100, 100);
    }

    [Theory]
    [InlineData(80, 0, 30, 10)]
    [InlineData(0, 80, 10, 30)]
    public void ValidateWithin_RejectsRegionOutsideImage(int x, int y, int width, int height)
    {
        OcrRegion region = new(x, y, width, height);

        Assert.Throws<ArgumentOutOfRangeException>(() => region.ValidateWithin(100, 100));
    }
}
