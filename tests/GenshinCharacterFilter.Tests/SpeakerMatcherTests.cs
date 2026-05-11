using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class SpeakerMatcherTests
{
    private const string WandererChinese = "\u6D41\u6D6A\u8005";
    private const string TravelerChinese = "\u65C5\u884C\u8005";

    [Fact]
    public void Match_MatchesChineseTargetSpeaker()
    {
        SpeakerMatchResult result = Match(WandererChinese);

        Assert.True(result.Matched);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesChineseTargetSpeakerWithColon()
    {
        SpeakerMatchResult result = Match($"{WandererChinese}\uFF1A");

        Assert.True(result.Matched);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesChineseTargetSpeakerWrappedInWhitespaceAndNewlines()
    {
        SpeakerMatchResult result = Match($" \r\n\t{WandererChinese}\n ");

        Assert.True(result.Matched);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesEnglishTargetSpeakerCaseInsensitively()
    {
        SpeakerMatchResult result = Match("wanderer");

        Assert.True(result.Matched);
        Assert.Equal("Wanderer", result.MatchedSpeaker);
        Assert.Equal("wanderer", result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesTextContainingTargetSpeaker()
    {
        SpeakerMatchResult result = Match($"\u5F53\u524D\u8BF4\u8BDD\u4EBA\uFF1A{WandererChinese}");

        Assert.True(result.Matched);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
    }

    [Fact]
    public void Match_DoesNotMatchDifferentChineseSpeaker()
    {
        SpeakerMatchResult result = Match(TravelerChinese);

        Assert.False(result.Matched);
        Assert.Null(result.MatchedSpeaker);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Match_DoesNotMatchEmptyText(string? text)
    {
        SpeakerMatchResult result = Match(text);

        Assert.False(result.Matched);
        Assert.Null(result.MatchedSpeaker);
        Assert.Equal(string.Empty, result.NormalizedText);
    }

    [Fact]
    public void Match_DoesNotMatchEmptyTargetList()
    {
        SpeakerMatchResult result = new SpeakerMatcher().Match(
            WandererChinese,
            new SpeakerMatcherOptions
            {
                TargetSpeakers = []
            });

        Assert.False(result.Matched);
        Assert.Null(result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    private static SpeakerMatchResult Match(string? text)
    {
        return new SpeakerMatcher().Match(
            text,
            new SpeakerMatcherOptions
            {
                TargetSpeakers = [WandererChinese, "Wanderer"]
            });
    }
}
