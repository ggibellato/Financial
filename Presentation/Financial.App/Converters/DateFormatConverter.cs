using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts DateTime to localized date string
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var effectiveCulture = CultureInfo.CurrentCulture;
            var format = parameter as string;
            if (string.IsNullOrWhiteSpace(format) || string.Equals(format, "d", StringComparison.OrdinalIgnoreCase))
            {
                format = GetPaddedShortDatePattern(effectiveCulture);
            }
            return dateTime.ToString(format, effectiveCulture);
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var effectiveCulture = CultureInfo.CurrentCulture;
        if (value is string str && DateTime.TryParse(str, effectiveCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        return DateTime.MinValue;
    }

    private static string GetPaddedShortDatePattern(CultureInfo culture)
    {
        return PadDayMonthTokens(culture.DateTimeFormat.ShortDatePattern);
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
                sb.Append(ch);
                if (i + 1 < pattern.Length && pattern[i + 1] == '\'')
                {
                    sb.Append(pattern[i + 1]);
                    i++;
                }
                else
                {
                    inQuote = !inQuote;
                }
                continue;
            }

            if (inQuote)
            {
                sb.Append(ch);
                continue;
            }

            if (ch == 'd' || ch == 'M')
            {
                int count = 1;
                while (i + count < pattern.Length && pattern[i + count] == ch)
                {
                    count++;
                }

                if (count == 1)
                {
                    sb.Append(ch, 2);
                }
                else
                {
                    sb.Append(ch, count);
                }

                i += count - 1;
                continue;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }
}


