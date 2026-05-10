using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrResultTests
{
    [Fact]
    public void Constructor_NormalizesTextWithTrimOnly()
    {
        OcrResult result = new("  raw text\r\n", "TesseractCli", "input.png");

        Assert.Equal("  raw text\r\n", result.RawText);
        Assert.Equal("raw text", result.NormalizedText);
        Assert.Equal("TesseractCli", result.EngineName);
        Assert.Equal("input.png", result.InputImagePath);
    }
}
