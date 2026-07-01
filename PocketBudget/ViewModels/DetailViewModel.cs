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

    [ObservableProperty]
    private string? receiptImagePath;

    [ObservableProperty]
    private bool hasReceiptImage;

    [ObservableProperty]
    private string receiptStatusMessage = "Sin ticket cargado.";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Expense", out var value) && value is Expense expense)
        {
            Description = expense.Description;
            Category = expense.Category;
            Amount = expense.Amount;
            Date = expense.Date;
            ReceiptImagePath = expense.ReceiptImagePath;
            HasReceiptImage = !string.IsNullOrWhiteSpace(expense.ReceiptImagePath);

            ReceiptStatusMessage = HasReceiptImage
                ? "Ticket asociado al gasto:"
                : "Este gasto no tiene ticket cargado.";
        }
    }
}