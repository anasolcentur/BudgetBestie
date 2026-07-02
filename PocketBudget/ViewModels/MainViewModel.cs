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
using System.Globalization;

namespace PocketBudget.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly List<Expense> _allExpenses = new();

    private Expense? _editingExpense;

    public MainViewModel()
        : this(
            new SqliteExpenseRepository(Path.Combine(FileSystem.AppDataDirectory, "expenses.db3")),
            new SqliteCategoryRepository(Path.Combine(FileSystem.AppDataDirectory, "expenses.db3")),
            new SqliteBudgetRepository(Path.Combine(FileSystem.AppDataDirectory, "expenses.db3")))
    {
        _ = LoadInitialDataAsync();
    }

    public MainViewModel(
        IExpenseRepository expenseRepository,
        ICategoryRepository categoryRepository,
        IBudgetRepository budgetRepository)
    {
        _expenseRepository = expenseRepository;
        _categoryRepository = categoryRepository;
        _budgetRepository = budgetRepository;
    }

    [ObservableProperty]
    private string statusMessage = "Cargá tus ingresos, categorías y gastos para empezar.";

    [ObservableProperty]
    private string monthlyIncomeText = string.Empty;

    [ObservableProperty]
    private decimal monthlyIncome;

    [ObservableProperty]
    private decimal monthlySpent;

    [ObservableProperty]
    private decimal availableBalance;

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
    private Category? selectedFilterCategory;

    [ObservableProperty]
    private string? receiptImagePath;

    [ObservableProperty]
    private string receiptStatusMessage = "Sin foto de ticket.";

    [ObservableProperty]
    private bool isEditingExpense;

    [ObservableProperty]
    private string expenseFormTitle = "Nuevo gastito 🛍️";

    [ObservableProperty]
    private string expenseSaveButtonText = "Agregar gasto 💖";

    public ObservableCollection<Category> Categories { get; } = new();

    public ObservableCollection<Category> FilterCategories { get; } = new();

    public ObservableCollection<Expense> Expenses { get; } = new();

    private async Task LoadInitialDataAsync()
    {
        await LoadBudgetAsync();
        await LoadCategoriesFromStorageAsync();
        await LoadSavedExpensesAsync();
    }

    private async Task LoadBudgetAsync()
    {
        try
        {
            MonthlyIncome = await _budgetRepository.GetMonthlyIncomeAsync();

            MonthlyIncomeText = MonthlyIncome > 0
                ? MonthlyIncome.ToString("F2", CultureInfo.CurrentCulture)
                : string.Empty;

            UpdateBudgetSummary();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo cargar el presupuesto: {ex.Message}";
        }
    }

    private async Task LoadCategoriesFromStorageAsync()
    {
        try
        {
            var previousFilterName = SelectedFilterCategory?.Name;

            var savedCategories = await _categoryRepository.GetCategoriesAsync();

            Categories.Clear();
            FilterCategories.Clear();

            FilterCategories.Add(new Category
            {
                Id = 0,
                Name = "Todas"
            });

            foreach (var category in savedCategories)
            {
                Categories.Add(category);
                FilterCategories.Add(category);
            }

            Categories.Add(new Category
            {
                Id = -1,
                Name = "➕ Crear nueva categoría"
            });

            SelectedFilterCategory = string.IsNullOrWhiteSpace(previousFilterName)
                ? FilterCategories.FirstOrDefault()
                : FilterCategories.FirstOrDefault(category =>
                    category.Name.Equals(previousFilterName, StringComparison.OrdinalIgnoreCase))
                  ?? FilterCategories.FirstOrDefault();
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

            _allExpenses.Clear();
            _allExpenses.AddRange(savedExpenses);

            ApplyExpenseFilter();
            UpdateBudgetSummary();

            if (_allExpenses.Count > 0)
            {
                StatusMessage = $"Se cargaron {_allExpenses.Count} gastos guardados.";
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

    partial void OnSelectedFilterCategoryChanged(Category? value)
    {
        ApplyExpenseFilter();
    }

    private void ApplyExpenseFilter()
    {
        Expenses.Clear();

        IEnumerable<Expense> filteredExpenses = _allExpenses;

        if (SelectedFilterCategory is not null && SelectedFilterCategory.Id != 0)
        {
            filteredExpenses = _allExpenses.Where(expense =>
                expense.Category.Equals(
                    SelectedFilterCategory.Name,
                    StringComparison.OrdinalIgnoreCase));
        }

        foreach (var expense in filteredExpenses)
        {
            Expenses.Add(expense);
        }
    }

    private void UpdateBudgetSummary()
    {
        var now = DateTime.Now;

        MonthlySpent = _allExpenses
            .Where(expense => expense.Date.Month == now.Month && expense.Date.Year == now.Year)
            .Sum(expense => expense.Amount);

        AvailableBalance = MonthlyIncome - MonthlySpent;
    }

    [RelayCommand]
    private async Task SaveMonthlyIncome()
    {
        if (!TryParseDecimal(MonthlyIncomeText, out var income) || income < 0)
        {
            StatusMessage = "Debe ingresar un ingreso mensual válido.";
            return;
        }

        try
        {
            await _budgetRepository.SaveMonthlyIncomeAsync(income);

            MonthlyIncome = income;
            MonthlyIncomeText = income.ToString("F2", CultureInfo.CurrentCulture);

            UpdateBudgetSummary();

            StatusMessage = "Ingreso mensual guardado correctamente ✨";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar el ingreso mensual: {ex.Message}";
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

        try
        {
            if (_editingExpense is not null)
            {
                await UpdateExistingExpenseAsync(amount);
            }
            else
            {
                await CreateNewExpenseAsync(amount);
            }

            ResetExpenseForm();
            ApplyExpenseFilter();
            UpdateBudgetSummary();

            VibrateOnSave();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo guardar el gasto: {ex.Message}";
        }
    }

    private async Task CreateNewExpenseAsync(decimal amount)
    {
        var expense = new Expense
        {
            Description = ExpenseDescription.Trim(),
            Amount = amount,
            Category = SelectedCategory!.Name,
            Date = DateTime.Now,
            ReceiptImagePath = ReceiptImagePath
        };

        await _expenseRepository.SaveExpenseAsync(expense);

        _allExpenses.Insert(0, expense);

        StatusMessage = "Gasto guardado correctamente 💖";
    }

    private async Task UpdateExistingExpenseAsync(decimal amount)
    {
        _editingExpense!.Description = ExpenseDescription.Trim();
        _editingExpense.Amount = amount;
        _editingExpense.Category = SelectedCategory!.Name;
        _editingExpense.ReceiptImagePath = ReceiptImagePath;

        await _expenseRepository.SaveExpenseAsync(_editingExpense);

        var index = _allExpenses.FindIndex(expense => expense.Id == _editingExpense.Id);

        if (index >= 0)
        {
            _allExpenses[index] = _editingExpense;
        }

        StatusMessage = "Gasto actualizado correctamente ✨";
    }

    [RelayCommand]
    private void EditExpense(Expense? expense)
    {
        if (expense is null)
        {
            return;
        }

        _editingExpense = expense;

        ExpenseDescription = expense.Description;
        ExpenseAmount = expense.Amount.ToString("F2", CultureInfo.CurrentCulture);
        SelectedCategory = Categories.FirstOrDefault(category =>
            category.Name.Equals(expense.Category, StringComparison.OrdinalIgnoreCase));
        ReceiptImagePath = expense.ReceiptImagePath;
        ReceiptStatusMessage = string.IsNullOrWhiteSpace(expense.ReceiptImagePath)
            ? "Sin foto de ticket."
            : "Ticket cargado para este gasto.";

        IsEditingExpense = true;
        ExpenseFormTitle = "Editar gastito ✏️";
        ExpenseSaveButtonText = "Guardar cambios ✨";
        StatusMessage = "Editando gasto seleccionado.";
    }

    [RelayCommand]
    private async Task DeleteExpense(Expense? expense)
    {
        if (expense is null)
        {
            return;
        }

        try
        {
            await _expenseRepository.DeleteExpenseAsync(expense);

            _allExpenses.RemoveAll(item => item.Id == expense.Id);

            if (_editingExpense?.Id == expense.Id)
            {
                ResetExpenseForm();
            }

            ApplyExpenseFilter();
            UpdateBudgetSummary();

            StatusMessage = "Gasto eliminado correctamente.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo eliminar el gasto: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        ResetExpenseForm();

        StatusMessage = "Edición cancelada.";
    }

    private void ResetExpenseForm()
    {
        _editingExpense = null;

        ExpenseDescription = string.Empty;
        ExpenseAmount = string.Empty;
        SelectedCategory = null;
        ReceiptImagePath = null;
        ReceiptStatusMessage = "Sin foto de ticket.";
        IsCategoryCreatorVisible = false;

        IsEditingExpense = false;
        ExpenseFormTitle = "Nuevo gastito 🛍️";
        ExpenseSaveButtonText = "Agregar gasto 💖";
    }

    private static bool TryParseDecimal(string value, out decimal amount)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
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