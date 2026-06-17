using System.Globalization;
using System.Text;

namespace Financial.Presentation.App.Helpers;

internal static class DateFormatHelper
{
    internal static string GetPaddedShortDatePattern()
    {
        var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        return PadDayMonthTokens(pattern);
    }

    private static string PadDayMonthTokens(string pattern)
    {
        var sb = new StringBuilder();
        bool inQuote = false;

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
        int count = 1;
        while (i + count < pattern.Length && pattern[i + count] == ch)
            count++;
        sb.Append(ch, count < 2 ? 2 : count);
        return i + count - 1;
    }
}
