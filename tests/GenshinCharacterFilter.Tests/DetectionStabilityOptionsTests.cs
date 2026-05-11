using GenshinCharacterFilter.Detection;

namespace GenshinCharacterFilter.Tests;

public sealed class DetectionStabilityOptionsTests
{
    [Fact]
    public void Defaults_AreConservative()
    {
        DetectionStabilityOptions options = new();

        Assert.Equal(2, options.MatchThreshold);
        Assert.Equal(2, options.MissThreshold);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void ValidateThreshold_AcceptsAllowedRange(int threshold)
    {
        DetectionStabilityOptions.ValidateThreshold(threshold, "threshold");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void ValidateThreshold_RejectsOutOfRangeValues(int threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DetectionStabilityOptions.ValidateThreshold(threshold, "threshold"));
    }
}
