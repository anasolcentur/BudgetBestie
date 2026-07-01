using CommunityToolkit.Mvvm.ComponentModel;
using PocketBudget.Models;

namespace PocketBudget.ViewModels;

public partial class DetailViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private DateTime date;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Expense", out var value) && value is Expense expense)
        {
            Description = expense.Description;
            Category = expense.Category;
            Amount = expense.Amount;
            Date = expense.Date;
        }
    }
}