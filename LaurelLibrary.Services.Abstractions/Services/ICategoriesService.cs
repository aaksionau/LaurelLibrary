using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Services;

public interface ICategoriesService
{
    /// <summary>
    /// Gets all categories for a library with pagination.
    /// </summary>
    Task<IEnumerable<Category>> GetAllCategoriesAsync(
        Guid libraryId,
        int page = 1,
        int pageSize = 20
    );

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    Task<Category?> GetCategoryByIdAsync(int id);

    /// <summary>
    /// Creates a new category.
    /// </summary>
    Task<Category> CreateCategoryAsync(Category category);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<Category?> UpdateCategoryAsync(Category category);

    /// <summary>
    /// Deletes a category.
    /// </summary>
    Task RemoveCategoryAsync(int categoryId);

    /// <summary>
    /// Gets a category by name for a specific library.
    /// </summary>
    Task<Category?> GetCategoryByNameAsync(string name, Guid libraryId);
}
