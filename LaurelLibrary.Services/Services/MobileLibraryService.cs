using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Dtos;
using LaurelLibrary.Services.Abstractions.Dtos.Mobile;
using LaurelLibrary.Services.Abstractions.Extensions;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class MobileLibraryService : IMobileLibraryService
{
    private readonly ILibrariesRepository _librariesRepository;
    private readonly ILogger<MobileLibraryService> _logger;

    public MobileLibraryService(
        ILibrariesRepository librariesRepository,
        ILogger<MobileLibraryService> logger
    )
    {
        _librariesRepository = librariesRepository;
        _logger = logger;
    }

    public async Task<List<MobileLibraryDto>> SearchLibrariesAsync(
        MobileLibrarySearchRequestDto request
    )
    {
        try
        {
            var libraries = await _librariesRepository.SearchLibrariesAsync(
                request.SearchTerm,
                request.City,
                request.State,
                request.MaxResults
            );

            return libraries
                .Select(l => new MobileLibraryDto
                {
                    LibraryId = l.LibraryId,
                    Name = l.Name,
                    Description = l.Description,
                    Address = l.Address,
                    CheckoutDurationDays = l.CheckoutDurationDays,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error searching libraries with search term: {SearchTerm}",
                request.SearchTerm
            );
            throw;
        }
    }

    public async Task<MobileLibraryDto?> GetLibraryByIdAsync(Guid libraryId)
    {
        try
        {
            var library = await _librariesRepository.GetByIdAsync(libraryId);
            if (library == null)
                return null;

            return new MobileLibraryDto
            {
                LibraryId = library.LibraryId,
                Name = library.Name,
                Description = library.Description,
                Address = library.Address,
                CheckoutDurationDays = library.CheckoutDurationDays,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting library by ID: {LibraryId}", libraryId);
            throw;
        }
    }
}
