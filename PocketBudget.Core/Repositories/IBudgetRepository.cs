namespace PocketBudget.Repositories;

public interface IBudgetRepository
{
    Task<decimal> GetMonthlyIncomeAsync();

    Task SaveMonthlyIncomeAsync(decimal monthlyIncome);
}