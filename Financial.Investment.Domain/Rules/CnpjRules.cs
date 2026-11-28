using System;
using System.Linq;

namespace Financial.Investment.Domain.Rules;

public static class CnpjRules
{
    public const string InvalidMessage = "Enter a valid 14-digit CNPJ.";

    private const int Length = 14;
    private static readonly int[] FirstCheckDigitWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondCheckDigitWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = new string(input.Where(c => !IsMaskCharacter(c)).ToArray());
        if (!IsValid(digits))
        {
            throw new ArgumentException(InvalidMessage);
        }

        return digits;
    }

    private static bool IsMaskCharacter(char c) => c is '.' or '/' or '-' || char.IsWhiteSpace(c);

    private static bool IsValid(string digits) =>
        digits.Length == Length
        && digits.All(char.IsAsciiDigit)
        && digits.Distinct().Count() > 1
        && digits[12] - '0' == CheckDigit(digits, FirstCheckDigitWeights)
        && digits[13] - '0' == CheckDigit(digits, SecondCheckDigitWeights);

    private static int CheckDigit(string digits, int[] weights)
    {
        var remainder = weights.Select((weight, i) => (digits[i] - '0') * weight).Sum() % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
