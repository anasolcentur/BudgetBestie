using PocketBudget.Models;

namespace PocketBudget.Repositories;

public interface IExpenseRepository
{
    Task InitializeAsync();

    Task<List<Expense>> GetExpensesAsync();

    Task<int> SaveExpenseAsync(Expense expense);

    Task<int> DeleteExpenseAsync(Expense expense);
}
