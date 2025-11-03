using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LaurelLibrary.UI.Extensions;

/// <summary>
/// HTML helper extensions for blob URLs
/// </summary>
public static class BlobUrlHtmlExtensions
{
    /// <summary>
    /// Gets the full blob URL from a relative path
    /// </summary>
    /// <param name="htmlHelper">The HTML helper</param>
    /// <param name="blobPath">The relative blob path</param>
    /// <returns>The full blob URL or empty string if path is null/empty</returns>
    public static string GetBlobUrl(this IHtmlHelper htmlHelper, string? blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return string.Empty;
        }

        var blobUrlService =
            htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IBlobUrlService>();

        return blobUrlService.GetFullBlobUrl(blobPath) ?? string.Empty;
    }

    /// <summary>
    /// Renders a book image with proper blob URL handling
    /// </summary>
    /// <param name="htmlHelper">The HTML helper</param>
    /// <param name="blobPath">The relative blob path</param>
    /// <param name="altText">Alternative text for the image</param>
    /// <param name="cssClass">CSS classes to apply</param>
    /// <param name="style">Inline styles to apply</param>
    /// <returns>HTML string for the image or placeholder</returns>
    public static IHtmlContent BookImage(
        this IHtmlHelper htmlHelper,
        string? blobPath,
        string altText = "Book cover",
        string? cssClass = null,
        string? style = null
    )
    {
        var fullUrl = htmlHelper.GetBlobUrl(blobPath);

        if (!string.IsNullOrWhiteSpace(fullUrl))
        {
            var imgTag = new TagBuilder("img");
            imgTag.Attributes["src"] = fullUrl;
            imgTag.Attributes["alt"] = altText;

            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                imgTag.AddCssClass(cssClass);
            }

            if (!string.IsNullOrWhiteSpace(style))
            {
                imgTag.Attributes["style"] = style;
            }

            return imgTag;
        }
        else
        {
            var divTag = new TagBuilder("div");
            divTag.AddCssClass("border bg-light d-flex align-items-center justify-content-center");

            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                divTag.AddCssClass(cssClass);
            }

            if (!string.IsNullOrWhiteSpace(style))
            {
                divTag.Attributes["style"] = style;
            }
            else
            {
                divTag.Attributes["style"] = "height:300px;";
            }

            var spanTag = new TagBuilder("span");
            spanTag.AddCssClass("text-muted");
            spanTag.InnerHtml.Append("No image");

            divTag.InnerHtml.AppendHtml(spanTag);

            return divTag;
        }
    }
}
