namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Performs simple rule-based speaker matching for debug and dry-run flows.
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
            return new SpeakerMatchResult(
                false,
                null,
                raw,
                normalizedText,
                normalizedText.Length == 0 ? SpeakerMatchKind.Unknown : SpeakerMatchKind.None);
        }

        StringComparison comparison = options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        foreach (string targetSpeaker in GetNormalizedTargetSpeakers(options.TargetSpeakers))
        {
            if (string.Equals(normalizedText, targetSpeaker, comparison))
            {
                return new SpeakerMatchResult(true, targetSpeaker, raw, normalizedText, SpeakerMatchKind.Strong);
            }
        }

        foreach (string targetSpeaker in GetNormalizedTargetSpeakers(options.TargetSpeakers))
        {
            if (normalizedText.Contains(targetSpeaker, comparison))
            {
                return new SpeakerMatchResult(true, targetSpeaker, raw, normalizedText, SpeakerMatchKind.Strong);
            }
        }

        foreach (string targetSpeaker in GetNormalizedTargetSpeakers(options.TargetSpeakers))
        {
            if (IsWeakTargetMatch(normalizedText, targetSpeaker, comparison))
            {
                return new SpeakerMatchResult(true, targetSpeaker, raw, normalizedText, SpeakerMatchKind.Weak);
            }
        }

        SpeakerMatchKind missKind = normalizedText.Length < 2
            ? SpeakerMatchKind.Unknown
            : SpeakerMatchKind.None;
        return new SpeakerMatchResult(false, null, raw, normalizedText, missKind);
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

    private static bool IsWeakTargetMatch(string normalizedText, string targetSpeaker, StringComparison comparison)
    {
        if (targetSpeaker.Length < 3 || !ContainsNonAscii(targetSpeaker))
        {
            return false;
        }

        foreach (string candidate in GetWeakMatchCandidates(normalizedText))
        {
            if (candidate.Length < 2)
            {
                continue;
            }

            if (Math.Abs(candidate.Length - targetSpeaker.Length) <= 1 &&
                CalculateEditDistance(candidate, targetSpeaker, comparison) <= 1)
            {
                return true;
            }

            if (GetLongestCommonSubstringLength(candidate, targetSpeaker, comparison) >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetWeakMatchCandidates(string normalizedText)
    {
        yield return normalizedText;

        char[] separators =
        [
            '\n', ' ', '\t',
            ':', '\uFF1A',
            ',', '\uFF0C',
            '.', '\u3002',
            ';', '\uFF1B'
        ];

        foreach (string part in normalizedText.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return part;
        }
    }

    private static int CalculateEditDistance(string left, string right, StringComparison comparison)
    {
        int[,] distances = new int[left.Length + 1, right.Length + 1];

        for (int i = 0; i <= left.Length; i++)
        {
            distances[i, 0] = i;
        }

        for (int j = 0; j <= right.Length; j++)
        {
            distances[0, j] = j;
        }

        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                int cost = CharactersEqual(left[i - 1], right[j - 1], comparison) ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[left.Length, right.Length];
    }

    private static int GetLongestCommonSubstringLength(string left, string right, StringComparison comparison)
    {
        int[,] lengths = new int[left.Length + 1, right.Length + 1];
        int longest = 0;

        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                if (!CharactersEqual(left[i - 1], right[j - 1], comparison))
                {
                    continue;
                }

                lengths[i, j] = lengths[i - 1, j - 1] + 1;
                longest = Math.Max(longest, lengths[i, j]);
            }
        }

        return longest;
    }

    private static bool CharactersEqual(char left, char right, StringComparison comparison)
    {
        return string.Equals(left.ToString(), right.ToString(), comparison);
    }

    private static bool ContainsNonAscii(string value)
    {
        return value.Any(character => character > 127);
    }
}
