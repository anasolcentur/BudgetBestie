using PocketBudget.Models;
using SQLite;

namespace PocketBudget.Repositories;

public class SqliteCategoryRepository : ICategoryRepository
{
    private readonly SQLiteAsyncConnection _database;
    private bool _initialized;

    private static readonly string[] DefaultCategories =
    {
        "Alimentos",
        "Transporte",
        "Ropa",
        "Salud",
        "Entretenimiento",
        "Educación",
        "Servicios",
        "Hogar",
        "Otros"
    };

    public SqliteCategoryRepository(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _database.CreateTableAsync<Category>();

        var existingCategories = await _database.Table<Category>().ToListAsync();

        if (existingCategories.Count == 0)
        {
            foreach (var categoryName in DefaultCategories)
            {
                await _database.InsertAsync(new Category
                {
                    Name = categoryName
                });
            }
        }

        _initialized = true;
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await InitializeAsync();

        return await _database
            .Table<Category>()
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<int> SaveCategoryAsync(Category category)
    {
        await InitializeAsync();

        var normalizedName = category.Name.Trim();

        var existingCategory = await _database
            .Table<Category>()
            .FirstOrDefaultAsync(existing =>
                existing.Name.ToLower() == normalizedName.ToLower());

        if (existingCategory is not null)
        {
            return 0;
        }

        category.Name = normalizedName;

        return await _database.InsertAsync(category);
    }
}