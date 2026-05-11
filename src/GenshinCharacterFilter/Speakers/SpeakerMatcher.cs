namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Performs simple rule-based speaker matching for v0.5 debug flows.
/// </summary>
public sealed class SpeakerMatcher : ISpeakerMatcher
{
    private static readonly char[] EdgeCharactersToTrim =
    [
        ' ', '\t', '\r', '\n',
        ':', '\uFF1A',
        '"', '\'', '`',
        '[', ']', '(', ')',
        '\u300C', '\u300D', '\u300E', '\u300F',
        '\uFF08', '\uFF09'
    ];

    public SpeakerMatchResult Match(string? rawText, SpeakerMatcherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string raw = rawText ?? string.Empty;
        string normalizedText = Normalize(raw);
        if (normalizedText.Length == 0 || options.TargetSpeakers.Count == 0)
        {
            return new SpeakerMatchResult(false, null, raw, normalizedText);
        }

        StringComparison comparison = options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        foreach (string targetSpeaker in GetNormalizedTargetSpeakers(options.TargetSpeakers))
        {
            if (string.Equals(normalizedText, targetSpeaker, comparison))
            {
                return new SpeakerMatchResult(true, targetSpeaker, raw, normalizedText);
            }
        }

        foreach (string targetSpeaker in GetNormalizedTargetSpeakers(options.TargetSpeakers))
        {
            if (normalizedText.Contains(targetSpeaker, comparison))
            {
                return new SpeakerMatchResult(true, targetSpeaker, raw, normalizedText);
            }
        }

        return new SpeakerMatchResult(false, null, raw, normalizedText);
    }

    /// <summary>
    /// Normalizes OCR text for deterministic debug matching.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalizedNewlines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        string[] lines = normalizedNewlines
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();

        return string.Join('\n', lines).Trim(EdgeCharactersToTrim);
    }

    private static IEnumerable<string> GetNormalizedTargetSpeakers(IEnumerable<string> targetSpeakers)
    {
        foreach (string? targetSpeaker in targetSpeakers)
        {
            string normalizedTarget = Normalize(targetSpeaker);
            if (normalizedTarget.Length > 0)
            {
                yield return normalizedTarget;
            }
        }
    }
}
