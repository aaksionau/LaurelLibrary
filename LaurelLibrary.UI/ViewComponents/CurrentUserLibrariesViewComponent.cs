using System;
using System.Collections.Generic;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Options;

namespace LaurelLibrary.UI.ViewComponents;

public class CurrentUserLibrariesViewComponent : ViewComponent
{
    private readonly IUserService userService;
    private readonly ILibrariesRepository librariesRepository;

    public CurrentUserLibrariesViewComponent(
        IUserService userService,
        ILibrariesRepository librariesRepository
    )
    {
        this.userService = userService;
        this.librariesRepository = librariesRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return View(Enumerable.Empty<SelectListItem>());
        }

        var user = await this.userService.GetAppUserAsync();
        var libraries =
            await this.librariesRepository.GetAllAsync(user.Id)
            ?? Enumerable.Empty<LibrarySummaryDto>();

        var selectedLibraryId = user.CurrentLibraryId.HasValue
            ? user.CurrentLibraryId.Value.ToString()
            : string.Empty;

        var options = new List<SelectListItem>
        {
            new SelectListItem() { Value = null, Text = "-- Select library --" },
        };
        options.AddRange(
            libraries.Select(l => new SelectListItem()
            {
                Value = l.LibraryId.ToString(),
                Text = l.Name,
                Selected = l.LibraryId.ToString() == selectedLibraryId,
            })
        );

        return View(options);
    }
}
