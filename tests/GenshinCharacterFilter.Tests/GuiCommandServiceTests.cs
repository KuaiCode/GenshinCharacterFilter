using GenshinCharacterFilter.Gui;

namespace GenshinCharacterFilter.Tests;

public sealed class GuiCommandServiceTests
{
    [Fact]
    public void GetDefaultOcrInputPath_UsesOriginalCapturePath()
    {
        string path = GuiCommandService.GetDefaultOcrInputPath();

        Assert.Equal(Path.Combine("debug-captures", "capture-latest.png"), path);
        Assert.DoesNotContain("debug-ocr", path, StringComparison.OrdinalIgnoreCase);
    }
}
