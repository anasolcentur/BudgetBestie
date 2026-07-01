using PocketBudget.Models;

namespace PocketBudget.Repositories;

public interface ICategoryRepository
{
    Task InitializeAsync();

    Task<List<Category>> GetCategoriesAsync();

    Task<int> SaveCategoryAsync(Category category);
}