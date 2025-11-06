using System;
using System.Net;
using System.Text.RegularExpressions;

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
}
