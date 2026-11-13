using EssayOrganizer.App.Models;

namespace EssayOrganizer.App.Services;

public class StatsService : IStatsService
{
    public EssayStats Calculate(string bodyText)
    {
        var text = bodyText ?? string.Empty;
        var normalized = text.Replace("\r\n", "\n");

        var lineCount = string.IsNullOrEmpty(normalized) ? 0 : normalized.Split('\n').Length;
        var paragraphCount = normalized
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Count(x => !string.IsNullOrWhiteSpace(x));

        return new EssayStats
        {
            CharacterCount = text.Length,
            CharacterCountWithoutSpaces = text.Count(ch => !char.IsWhiteSpace(ch)),
            LineCount = lineCount,
            ParagraphCount = paragraphCount
        };
    }
}

