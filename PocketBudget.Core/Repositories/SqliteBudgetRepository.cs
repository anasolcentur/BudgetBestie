using PocketBudget.Models;
using SQLite;

namespace PocketBudget.Repositories;

public class SqliteBudgetRepository : IBudgetRepository
{
    private readonly SQLiteAsyncConnection _database;
    private bool _initialized;

    public SqliteBudgetRepository(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath);
    }

    private async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _database.CreateTableAsync<BudgetSetting>();
        _initialized = true;
    }

    public async Task<decimal> GetMonthlyIncomeAsync()
    {
        await InitializeAsync();

        var setting = await _database
            .Table<BudgetSetting>()
            .FirstOrDefaultAsync(item => item.Id == 1);

        return setting?.MonthlyIncome ?? 0;
    }

    public async Task SaveMonthlyIncomeAsync(decimal monthlyIncome)
    {
        await InitializeAsync();

        var setting = new BudgetSetting
        {
            Id = 1,
            MonthlyIncome = monthlyIncome
        };

        await _database.InsertOrReplaceAsync(setting);
    }
}