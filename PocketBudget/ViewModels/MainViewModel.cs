using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
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

    [ObservableProperty]
    private string? receiptImagePath;

    [ObservableProperty]
    private string receiptStatusMessage = "Sin foto de ticket.";

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
    private async Task CaptureReceipt()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ReceiptStatusMessage = "La cámara no está disponible en este dispositivo.";
                return;
            }

            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (permissionStatus != PermissionStatus.Granted)
            {
                ReceiptStatusMessage = "No se otorgó permiso para usar la cámara.";
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo is null)
            {
                ReceiptStatusMessage = "No se tomó ninguna foto.";
                return;
            }

            var fileName = $"ticket_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await using var sourceStream = await photo.OpenReadAsync();
            await using var localFileStream = File.OpenWrite(localPath);

            await sourceStream.CopyToAsync(localFileStream);

            ReceiptImagePath = localPath;
            ReceiptStatusMessage = "Ticket agregado correctamente 📸";
        }
        catch (PermissionException)
        {
            ReceiptStatusMessage = "No se pudo acceder a la cámara por permisos.";
        }
        catch (Exception ex)
        {
            ReceiptStatusMessage = $"No se pudo guardar la foto del ticket: {ex.Message}";
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
            Date = DateTime.Now,
            ReceiptImagePath = ReceiptImagePath
        };

        try
        {
            await _expenseRepository.SaveExpenseAsync(expense);

            Expenses.Insert(0, expense);

            ExpenseDescription = string.Empty;
            ExpenseAmount = string.Empty;
            SelectedCategory = null;
            ReceiptImagePath = null;
            ReceiptStatusMessage = "Sin foto de ticket.";

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