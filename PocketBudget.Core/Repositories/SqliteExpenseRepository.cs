using PocketBudget.Models;
using SQLite;

namespace PocketBudget.Repositories;

public class SqliteExpenseRepository : IExpenseRepository
{
    private readonly SQLiteAsyncConnection _database;
    private bool _initialized;

    public SqliteExpenseRepository(string databasePath)
    {
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _database.CreateTableAsync<Expense>();
        _initialized = true;
    }

    public async Task<List<Expense>> GetExpensesAsync()
    {
        await InitializeAsync();

        return await _database
            .Table<Expense>()
            .OrderByDescending(expense => expense.Date)
            .ToListAsync();
    }

    public async Task<int> SaveExpenseAsync(Expense expense)
    {
        await InitializeAsync();

        if (expense.Id != 0)
        {
            return await _database.UpdateAsync(expense);
        }

        return await _database.InsertAsync(expense);
    }

    public async Task<int> DeleteExpenseAsync(Expense expense)
    {
        await InitializeAsync();

        return await _database.DeleteAsync(expense);
    }
}