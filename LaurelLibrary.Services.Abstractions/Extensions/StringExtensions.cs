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

        // Decode HTML entities, then remove tags
        var decoded = WebUtility.HtmlDecode(input);
        var noTags = Regex.Replace(decoded, "<.*?>", string.Empty);
        return noTags?.Trim() ?? string.Empty;
    }
}
