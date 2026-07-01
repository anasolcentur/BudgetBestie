using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PocketBudget.Models;
using PocketBudget.Services;
using PocketBudget.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace PocketBudget.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public MainViewModel()
    {
        _apiService = new ApiService();
    }

    [ObservableProperty]
    private string statusMessage = "Cargá las categorías para empezar a registrar tus gastos.";

    [ObservableProperty]
    private string expenseDescription = string.Empty;

    [ObservableProperty]
    private string expenseAmount = string.Empty;

    [ObservableProperty]
    private Category? selectedCategory;

    public ObservableCollection<Category> Categories { get; } = new();

    public ObservableCollection<Expense> Expenses { get; } = new();

    [RelayCommand]
    private async Task LoadCategories()
    {
        try
        {
            StatusMessage = "Cargando categorías desde la API...";

            Categories.Clear();

            var categories = await _apiService.GetCategoriesAsync();

            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            StatusMessage = $"Listo bestie: se cargaron {Categories.Count} categorías.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode is not null)
        {
            StatusMessage = $"Error HTTP {(int)ex.StatusCode}: no se pudieron cargar las categorías.";
        }
        catch (HttpRequestException)
        {
            StatusMessage = "Error de conexión: revisá tu acceso a Internet.";
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "La solicitud tardó demasiado. Intentá nuevamente.";
        }
        catch (JsonException)
        {
            StatusMessage = "Error al procesar los datos recibidos desde la API.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error inesperado: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddExpense()
    {
        if (string.IsNullOrWhiteSpace(ExpenseDescription))
        {
            StatusMessage = "Debe ingresar una descripción.";
            return;
        }

        if (!TryParseAmount(ExpenseAmount, out var amount) || amount <= 0)
        {
            StatusMessage = "Debe ingresar un monto válido mayor a cero.";
            return;
        }

        if (SelectedCategory is null)
        {
            StatusMessage = "Debe seleccionar una categoría.";
            return;
        }

        Expenses.Add(new Expense
        {
            Description = ExpenseDescription.Trim(),
            Amount = amount,
            Category = SelectedCategory.Name,
            Date = DateTime.Now
        });

        ExpenseDescription = string.Empty;
        ExpenseAmount = string.Empty;
        SelectedCategory = null;

        StatusMessage = "Gasto agregado correctamente 💖";
    }

    [RelayCommand]
    private async Task GoToDetail(Expense? expense)
    {
        if (expense is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(DetailPage), new Dictionary<string, object>
        {
            { "Expense", expense }
        });
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}