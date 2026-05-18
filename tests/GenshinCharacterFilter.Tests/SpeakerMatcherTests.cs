using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class SpeakerMatcherTests
{
    private const string WandererChinese = "\u6D41\u6D6A\u8005";
    private const string TravelerChinese = "\u65C5\u884C\u8005";
    private const string ClorindeChinese = "\u514B\u6D1B\u7433\u5FB7";

    [Fact]
    public void Match_MatchesChineseTargetSpeaker()
    {
        SpeakerMatchResult result = Match(WandererChinese);

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesChineseTargetSpeakerWithColon()
    {
        SpeakerMatchResult result = Match($"{WandererChinese}\uFF1A");

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesChineseTargetSpeakerWithAsciiColon()
    {
        SpeakerMatchResult result = Match($"{WandererChinese}:");

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesChineseTargetSpeakerWrappedInWhitespaceAndNewlines()
    {
        SpeakerMatchResult result = Match($" \r\n\t{WandererChinese}\n ");

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
        Assert.Equal(WandererChinese, result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesEnglishTargetSpeakerCaseInsensitively()
    {
        SpeakerMatchResult result = Match("wanderer");

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal("Wanderer", result.MatchedSpeaker);
        Assert.Equal("wanderer", result.NormalizedText);
    }

    [Fact]
    public void Match_MatchesTextContainingTargetSpeakerForDebugOnly()
    {
        SpeakerMatchResult result = Match($"\u5F53\u524D\u8BF4\u8BDD\u4EBA\uFF1A{WandererChinese}");

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal(WandererChinese, result.MatchedSpeaker);
    }

    [Fact]
    public void Match_StrongMatchesClorinde()
    {
        SpeakerMatchResult result = MatchClorinde(ClorindeChinese);

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Strong, result.MatchKind);
        Assert.Equal(ClorindeChinese, result.MatchedSpeaker);
    }

    [Theory]
    [InlineData("\u5492\u6D1B\u7433\u5FB7")]
    [InlineData("\u5144\u6D1B\u7433\u5FB7")]
    [InlineData("\u6D1B\u7433\u5FB7")]
    public void Match_NearClorindeOcrTextIsWeakMatch(string text)
    {
        SpeakerMatchResult result = MatchClorinde(text);

        Assert.True(result.Matched);
        Assert.Equal(SpeakerMatchKind.Weak, result.MatchKind);
        Assert.Equal(ClorindeChinese, result.MatchedSpeaker);
    }

    [Fact]
    public void Match_StrongMissDoesNotMatchDifferentChineseSpeaker()
    {
        SpeakerMatchResult result = MatchClorinde("\u91CD\u4E91");

        Assert.False(result.Matched);
        Assert.Equal(SpeakerMatchKind.None, result.MatchKind);
        Assert.Null(result.MatchedSpeaker);
    }

    [Fact]
    public void Match_DoesNotMatchDifferentChineseSpeaker()
    {
        SpeakerMatchResult result = Match(TravelerChinese);

        Assert.False(result.Matched);
        Assert.Equal(SpeakerMatchKind.None, result.MatchKind);
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
        Assert.Equal(SpeakerMatchKind.Unknown, result.MatchKind);
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
        Assert.Equal(SpeakerMatchKind.None, result.MatchKind);
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

    private static SpeakerMatchResult MatchClorinde(string? text)
    {
        return new SpeakerMatcher().Match(
            text,
            new SpeakerMatcherOptions
            {
                TargetSpeakers = [ClorindeChinese]
            });
    }
}
