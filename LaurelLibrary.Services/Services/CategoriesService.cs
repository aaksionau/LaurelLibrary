using LaurelLibrary.Domain.Entities;
using LaurelLibrary.Services.Abstractions.Repositories;
using LaurelLibrary.Services.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace LaurelLibrary.Services.Services;

public class CategoriesService : ICategoriesService
{
    private readonly ICategoriesRepository _categoriesRepository;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(
        ICategoriesRepository categoriesRepository,
        ILogger<CategoriesService> logger
    )
    {
        _categoriesRepository = categoriesRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 20
    )
    {
        return await _categoriesRepository.GetAllAsync(libraryId, page, pageSize);
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _categoriesRepository.GetByIdAsync(id);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        return await _categoriesRepository.CreateAsync(category);
    }

    public async Task<Category?> UpdateCategoryAsync(Category category)
    {
        return await _categoriesRepository.UpdateAsync(category);
    }

    public async Task RemoveCategoryAsync(int categoryId)
    {
        await _categoriesRepository.RemoveAsync(categoryId);
    }

    public async Task<Category?> GetCategoryByNameAsync(string name, Guid libraryId)
    {
        return await _categoriesRepository.GetByNameAsync(name, libraryId);
    }
}
