using System;
using System.Collections.Generic;
using LaurelLibrary.Domain.Entities;

namespace LaurelLibrary.Services.Abstractions.Repositories;

public interface ICategoriesRepository
{
    Task<Category> CreateAsync(Category category);
    Task<Category?> GetByIdAsync(int id);
    Task<Category?> GetByNameAsync(string name, Guid libraryId);
    Task<IEnumerable<Category>> GetAllAsync(Guid libraryId, int page = 1, int pageSize = 20);
    Task<Category?> UpdateAsync(Category category);
    Task RemoveAsync(int categoryId);
    Task<IEnumerable<Category>> SearchByNameAsync(
        string searchTerm,
        Guid libraryId,
        int limit = 10
    );
}
