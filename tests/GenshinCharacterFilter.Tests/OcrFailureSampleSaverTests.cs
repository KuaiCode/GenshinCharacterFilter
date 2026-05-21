using System.Drawing;
using System.Text.Json;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrFailureSampleSaverTests
{
    [Fact]
    public void Save_WritesImageAndMetadata()
    {
        using TempImage image = TempImage.Create();
        string outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            OcrFailureSampleSaver saver = new(outputDirectory);
            OcrFailureSampleResult result = saver.Save(
                image.Path,
                new OcrFailureSampleMetadata(
                    DateTimeOffset.Parse("2026-05-20T12:00:00+08:00"),
                    "TesseractCli",
                    string.Empty,
                    string.Empty,
                    ["流浪者"],
                    new OcrRegion(1, 2, 3, 4),
                    1234,
                    7));

            Assert.True(File.Exists(result.ImagePath));
            Assert.True(File.Exists(result.MetadataPath));
            string json = File.ReadAllText(result.MetadataPath);
            using JsonDocument document = JsonDocument.Parse(json);
            Assert.Equal("TesseractCli", document.RootElement.GetProperty("OcrEngine").GetString());
            Assert.Equal(7, document.RootElement.GetProperty("Iteration").GetInt32());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ShouldSave_ReturnsTrueForUnknownOrMiss()
    {
        Assert.True(OcrFailureSampleSaver.ShouldSave(
            new SpeakerMatchResult(false, null, string.Empty, string.Empty, SpeakerMatchKind.Unknown)));
        Assert.True(OcrFailureSampleSaver.ShouldSave(
            new SpeakerMatchResult(false, null, "noise", "noise", SpeakerMatchKind.None)));
    }

    [Fact]
    public void ShouldSave_ReturnsFalseForMatchedText()
    {
        Assert.False(OcrFailureSampleSaver.ShouldSave(
            new SpeakerMatchResult(true, "流浪者", "流浪者", "流浪者", SpeakerMatchKind.Strong)));
    }

    private sealed class TempImage : IDisposable
    {
        private TempImage(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempImage Create()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            using Bitmap bitmap = new(4, 4);
            bitmap.Save(path);
            return new TempImage(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
