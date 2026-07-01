using PocketBudget.Models;
using PocketBudget.Repositories;

namespace PocketBudget.Tests;

public class ExpenseRepositoryTests
{
    public ExpenseRepositoryTests()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    [Fact]
    public async Task SaveExpenseAsync_WhenUsingInMemoryDatabase_SavesAndReturnsExpense()
    {
        var repository = new SqliteExpenseRepository(":memory:");

        var expense = new Expense
        {
            Description = "Café",
            Amount = 2500,
            Category = "Alimentos",
            Date = new DateTime(2026, 7, 1, 10, 0, 0)
        };

        await repository.SaveExpenseAsync(expense);

        var expenses = await repository.GetExpensesAsync();

        Assert.Single(expenses);
        Assert.True(expenses[0].Id > 0);
        Assert.Equal("Café", expenses[0].Description);
        Assert.Equal(2500, expenses[0].Amount);
        Assert.Equal("Alimentos", expenses[0].Category);
    }
}