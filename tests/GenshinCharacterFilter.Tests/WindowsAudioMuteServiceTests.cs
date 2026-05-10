using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Tests;

public sealed class WindowsAudioMuteServiceTests
{
    [Theory]
    [InlineData("GenshinImpact", "GenshinImpact")]
    [InlineData("GenshinImpact.exe", "GenshinImpact")]
    [InlineData("  YuanShen.exe  ", "YuanShen")]
    [InlineData("game.EXE", "game")]
    public void NormalizeProcessName_RemovesOptionalExeExtension(string input, string expected)
    {
        Assert.Equal(expected, WindowsAudioMuteService.NormalizeProcessName(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".exe")]
    public void NormalizeProcessName_RejectsBlankNames(string input)
    {
        Assert.Throws<ArgumentException>(() => WindowsAudioMuteService.NormalizeProcessName(input));
    }
}
