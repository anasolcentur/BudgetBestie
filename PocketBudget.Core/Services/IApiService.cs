using PocketBudget.Models;

namespace PocketBudget.Services;

public interface IApiService
{
    Task<List<Category>> GetCategoriesAsync();
}