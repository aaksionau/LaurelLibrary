using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaurelLibrary.UI.ViewComponents;

/// <summary>
/// View component for rendering book images with full URLs
/// </summary>
public class BookImageViewComponent : ViewComponent
{
    private readonly IBlobUrlService _blobUrlService;

    public BookImageViewComponent(IBlobUrlService blobUrlService)
    {
        _blobUrlService = blobUrlService;
    }

    /// <summary>
    /// Invokes the book image view component
    /// </summary>
    /// <param name="imagePath">The relative image path from the database</param>
    /// <param name="altText">Alternative text for the image</param>
    /// <param name="cssClass">CSS classes to apply to the image</param>
    /// <param name="style">Inline styles to apply to the image</param>
    /// <returns>View component result</returns>
    public IViewComponentResult Invoke(
        string? imagePath,
        string altText = "Book cover",
        string? cssClass = null,
        string? style = null
    )
    {
        var model = new BookImageViewModel
        {
            ImageUrl = _blobUrlService.GetFullBlobUrl(imagePath),
            AltText = altText,
            CssClass = cssClass,
            Style = style,
        };

        return View(model);
    }
}

/// <summary>
/// View model for book image component
/// </summary>
public class BookImageViewModel
{
    public string? ImageUrl { get; set; }
    public string AltText { get; set; } = "Book cover";
    public string? CssClass { get; set; }
    public string? Style { get; set; }
}
