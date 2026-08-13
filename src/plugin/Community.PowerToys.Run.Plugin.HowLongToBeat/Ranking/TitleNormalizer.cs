using System.Globalization;
using System.Text;

namespace Community.PowerToys.Run.Plugin.HowLongToBeat.Ranking;

public static class TitleNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed =
            value.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder(
                decomposed.Length);

        var previousWasSpace = true;

        foreach (var character in decomposed)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(
                    char.ToLowerInvariant(
                        character));

                previousWasSpace = false;
                continue;
            }

            if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder
            .ToString()
            .Trim();
    }

    public static string[] Tokens(
        string? value)
    {
        var normalized =
            Normalize(value);

        if (normalized.Length == 0)
        {
            return [];
        }

        return normalized.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);
    }
}