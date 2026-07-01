using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using PocketBudget.Models;
using PocketBudget.Repositories;
using PocketBudget.Services;
using PocketBudget.Validators;
using PocketBudget.Views;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace PocketBudget.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IExpenseRepository _expenseRepository;

    public MainViewModel()
        : this(
            new ApiService(),
            new SqliteExpenseRepository(Path.Combine(FileSystem.AppDataDirectory, "expenses.db3")))
    {
        _ = LoadSavedExpensesAsync();
    }

    public MainViewModel(IApiService apiService, IExpenseRepository expenseRepository)
    {
        _apiService = apiService;
        _expenseRepository = expenseRepository;
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

    private async Task LoadSavedExpensesAsync()
    {
        try
        {
            var savedExpenses = await _expenseRepository.GetExpensesAsync();

            Expenses.Clear();

            foreach (var expense in savedExpenses)
            {
                Expenses.Add(expense);
            }

            if (Expenses.Count > 0)
            {
                StatusMessage = $"Se cargaron {Expenses.Count} gastos guardados.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudieron cargar los gastos guardados: {ex.Message}";
        }
    }

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
    private async Task AddExpense()
    {
        var isValid = ExpenseValidator.TryValidate(
            ExpenseDescription,
            ExpenseAmount,
            SelectedCategory,
            out var amount,
            out var errorMessage);

        if (!isValid)
        {
            StatusMessage = errorMessage;
            return;
        }

        var expense = new Expense
        {
            Description = ExpenseDescription.Trim(),
            Amount = amount,
            Category = SelectedCategory!.Name,
            Date = DateTime.Now
        };

        try
        {
            await _expenseRepository.SaveExpenseAsync(expense);

            Expenses.Insert(0, expense);

            ExpenseDescription = string.Empty;
            ExpenseAmount = string.Empty;
            SelectedCategory = null;

            StatusMessage = "Gasto guardado correctamente 💖";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar el gasto: {ex.Message}";
        }
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
}