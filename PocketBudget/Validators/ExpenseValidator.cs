using PocketBudget.Models;
using System.Globalization;

namespace PocketBudget.Validators;

public static class ExpenseValidator
{
    public static bool TryValidate(
        string description,
        string amountText,
        Category? selectedCategory,
        out decimal amount,
        out string errorMessage)
    {
        amount = 0;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(description))
        {
            errorMessage = "Debe ingresar una descripción.";
            return false;
        }

        if (!TryParseAmount(amountText, out amount) || amount <= 0)
        {
            errorMessage = "Debe ingresar un monto válido mayor a cero.";
            return false;
        }

        if (selectedCategory is null)
        {
            errorMessage = "Debe seleccionar una categoría.";
            return false;
        }

        return true;
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
            || decimal.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}