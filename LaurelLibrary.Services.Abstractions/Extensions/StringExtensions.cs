using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;

namespace LaurelLibrary.Services.Abstractions.Extensions;

public static class StringExtensions
{
    public static string StripHtml(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Decode HTML entities first
        var decoded = WebUtility.HtmlDecode(input);

        // Convert HTML line break elements to actual line breaks
        var withLineBreaks = Regex.Replace(decoded, @"<br\s*/?>\s*", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(
            withLineBreaks,
            @"<p\s*/?>\s*",
            "\n",
            RegexOptions.IgnoreCase
        );
        withLineBreaks = Regex.Replace(withLineBreaks, @"</p>\s*", "\n", RegexOptions.IgnoreCase);
        withLineBreaks = Regex.Replace(
            withLineBreaks,
            @"<div\s*[^>]*>\s*",
            "\n",
            RegexOptions.IgnoreCase
        );
        withLineBreaks = Regex.Replace(withLineBreaks, @"</div>\s*", "\n", RegexOptions.IgnoreCase);

        // Remove all remaining HTML tags
        var noTags = Regex.Replace(withLineBreaks, "<.*?>", string.Empty);

        // Clean up multiple consecutive line breaks and trim
        var cleaned = Regex.Replace(noTags, @"\n\s*\n\s*\n+", "\n\n");
        return cleaned?.Trim() ?? string.Empty;
    }

    public static string NormalizeIsbn(this string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return string.Empty;
        }

        // Remove all non-digit characters (hyphens, spaces, etc.) except X for ISBN-10
        var digits = Regex.Replace(isbn.ToUpperInvariant(), @"[^0-9X]", string.Empty);

        switch (digits.Length)
        {
            case 9:
                // Calculate ISBN-10 checksum for 9 digits
                var isbn10 = CalculateIsbn10Checksum(digits);
                return ConvertIsbn10ToIsbn13(isbn10);
            case 10:
                // Convert ISBN-10 to ISBN-13 by adding 978 prefix
                return ConvertIsbn10ToIsbn13(digits);
            case 13:
                // Return ISBN-13 as is (could validate checksum here if needed)
                return digits;
            default:
                // Invalid ISBN length
                return digits;
        }
    }

    private static string CalculateIsbn10Checksum(string nineDigits)
    {
        if (nineDigits.Length != 9)
            return nineDigits;

        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (i + 1) * (nineDigits[i] - '0');
        }

        int checksum = sum % 11;
        string checksumChar = checksum == 10 ? "X" : checksum.ToString();

        return nineDigits + checksumChar;
    }

    private static string ConvertIsbn10ToIsbn13(string isbn10)
    {
        if (isbn10.Length != 10)
            return isbn10;

        // Take first 9 digits and add 978 prefix
        string isbn13WithoutChecksum = "978" + isbn10.Substring(0, 9);

        // Calculate ISBN-13 checksum
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = isbn13WithoutChecksum[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checksum = (10 - (sum % 10)) % 10;

        return isbn13WithoutChecksum + checksum.ToString();
    }
}
