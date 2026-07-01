using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using PocketBudget.Models;
using PocketBudget.Repositories;
using PocketBudget.Validators;
using PocketBudget.Views;
using System.Collections.ObjectModel;

namespace PocketBudget.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;

    public MainViewModel()
        : this(
            new SqliteExpenseRepository(Path.Combine(FileSystem.AppDataDirectory, "expenses.db3")),
            new SqliteCategoryRepository(Path.Combine(FileSystem.AppDataDirectory, "expenses.db3")))
    {
        _ = LoadInitialDataAsync();
    }

    public MainViewModel(
        IExpenseRepository expenseRepository,
        ICategoryRepository categoryRepository)
    {
        _expenseRepository = expenseRepository;
        _categoryRepository = categoryRepository;
    }

    [ObservableProperty]
    private string statusMessage = "Cargá tus ingresos, categorías y gastos para empezar.";

    [ObservableProperty]
    private string expenseDescription = string.Empty;

    [ObservableProperty]
    private string expenseAmount = string.Empty;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private string newCategoryName = string.Empty;

    [ObservableProperty]
    private bool isCategoryCreatorVisible;

    [ObservableProperty]
    private string? receiptImagePath;

    [ObservableProperty]
    private string receiptStatusMessage = "Sin foto de ticket.";

    public ObservableCollection<Category> Categories { get; } = new();

    public ObservableCollection<Expense> Expenses { get; } = new();

    private async Task LoadInitialDataAsync()
    {
        await LoadCategoriesFromStorageAsync();
        await LoadSavedExpensesAsync();
    }

    private async Task LoadCategoriesFromStorageAsync()
    {
        try
        {
            var savedCategories = await _categoryRepository.GetCategoriesAsync();

            Categories.Clear();

            foreach (var category in savedCategories)
            {
                Categories.Add(category);
            }

            Categories.Add(new Category
            {
                Id = -1,
                Name = "➕ Crear nueva categoría"
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudieron cargar las categorías: {ex.Message}";
        }
    }

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

    partial void OnSelectedCategoryChanged(Category? value)
    {
        IsCategoryCreatorVisible = value?.Id == -1;

        if (IsCategoryCreatorVisible)
        {
            StatusMessage = "Ingresá el nombre de la nueva categoría.";
        }
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            StatusMessage = "Debe ingresar el nombre de la categoría.";
            return;
        }

        var categoryName = NewCategoryName.Trim();

        try
        {
            var result = await _categoryRepository.SaveCategoryAsync(new Category
            {
                Name = categoryName
            });

            await LoadCategoriesFromStorageAsync();

            SelectedCategory = Categories.FirstOrDefault(category =>
                category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            NewCategoryName = string.Empty;
            IsCategoryCreatorVisible = false;

            StatusMessage = result == 0
                ? "La categoría ya existía y fue seleccionada."
                : "Categoría agregada correctamente ✨";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar la categoría: {ex.Message}";
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

        if (SelectedCategory?.Id == -1)
        {
            StatusMessage = "Debe seleccionar una categoría válida.";
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
            IsCategoryCreatorVisible = false;

            VibrateOnSave();

            StatusMessage = "Gasto guardado correctamente 💖";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar el gasto: {ex.Message}";
        }
    }

    private static void VibrateOnSave()
    {
        try
        {
            if (Vibration.Default.IsSupported)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(250));
            }
        }
        catch
        {
            // Si el dispositivo no permite vibración, la app continúa funcionando.
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