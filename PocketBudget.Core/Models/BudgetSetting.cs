using SQLite;

namespace PocketBudget.Models;

public class BudgetSetting
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public decimal MonthlyIncome { get; set; }
}