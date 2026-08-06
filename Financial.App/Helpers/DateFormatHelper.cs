using System.Globalization;
using System.Text;

namespace Financial.Presentation.App.Helpers;

public static class DateFormatHelper
{
    public static string GetPaddedShortDatePattern()
    {
        var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        return PadDayMonthTokens(pattern);
    }

    public static string GetMonthYearPattern() => StripDayToken(GetPaddedShortDatePattern());

    private static string StripDayToken(string pattern)
    {
        var dayStart = pattern.IndexOf('d');
        if (dayStart < 0)
        {
            return pattern;
        }

        var dayEnd = dayStart;
        while (dayEnd < pattern.Length && pattern[dayEnd] == 'd')
        {
            dayEnd++;
        }

        var before = pattern[..dayStart];
        var after = pattern[dayEnd..];
        return after.Length > 0
            ? before + after.TrimStart('/', '.', '-', ' ')
            : before.TrimEnd('/', '.', '-', ' ');
    }

    private static string PadDayMonthTokens(string pattern)
    {
        var sb = new StringBuilder();
        var inQuote = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            if (ch == '\'')
            {
                i = HandleQuoteChar(pattern, i, sb, ref inQuote);
                continue;
            }
            if (inQuote) { sb.Append(ch); continue; }
            if (ch == 'd' || ch == 'M')
            {
                i = HandleFormatToken(pattern, i, ch, sb);
                continue;
            }
            sb.Append(ch);
        }

        return sb.ToString();
    }

    private static int HandleQuoteChar(string pattern, int i, StringBuilder sb, ref bool inQuote)
    {
        sb.Append('\'');
        if (i + 1 < pattern.Length && pattern[i + 1] == '\'')
        {
            sb.Append(pattern[i + 1]);
            return i + 1;
        }
        inQuote = !inQuote;
        return i;
    }

    private static int HandleFormatToken(string pattern, int i, char ch, StringBuilder sb)
    {
        int j = i;
        while (j < pattern.Length && pattern[j] == ch)
            j++;
        var count = j - i;
        sb.Append(new string(ch, count < 2 ? 2 : count));
        return j - 1;
    }
}
