using PocketBudget.Models;
using PocketBudget.Validators;

namespace PocketBudget.Tests;

public class ExpenseValidatorTests
{
    [Fact]
    public void TryValidate_WhenDescriptionIsEmpty_ReturnsFalse()
    {
        var category = new Category { Name = "Electrónica" };

        var result = ExpenseValidator.TryValidate(
            "",
            "1500",
            category,
            out var amount,
            out var errorMessage);

        Assert.False(result);
        Assert.Equal(0, amount);
        Assert.Equal("Debe ingresar una descripción.", errorMessage);
    }

    [Fact]
    public void TryValidate_WhenAmountIsInvalid_ReturnsFalse()
    {
        var category = new Category { Name = "Electrónica" };

        var result = ExpenseValidator.TryValidate(
            "Auriculares",
            "abc",
            category,
            out var amount,
            out var errorMessage);

        Assert.False(result);
        Assert.Equal(0, amount);
        Assert.Equal("Debe ingresar un monto válido mayor a cero.", errorMessage);
    }

    [Fact]
    public void TryValidate_WhenAmountIsZero_ReturnsFalse()
    {
        var category = new Category { Name = "Electrónica" };

        var result = ExpenseValidator.TryValidate(
            "Auriculares",
            "0",
            category,
            out var amount,
            out var errorMessage);

        Assert.False(result);
        Assert.Equal(0, amount);
        Assert.Equal("Debe ingresar un monto válido mayor a cero.", errorMessage);
    }

    [Fact]
    public void TryValidate_WhenCategoryIsNull_ReturnsFalse()
    {
        var result = ExpenseValidator.TryValidate(
            "Auriculares",
            "1500",
            null,
            out var amount,
            out var errorMessage);

        Assert.False(result);
        Assert.Equal(1500, amount);
        Assert.Equal("Debe seleccionar una categoría.", errorMessage);
    }

    [Fact]
    public void TryValidate_WhenDataIsValid_ReturnsTrue()
    {
        var category = new Category { Name = "Electrónica" };

        var result = ExpenseValidator.TryValidate(
            "Auriculares",
            "1500",
            category,
            out var amount,
            out var errorMessage);

        Assert.True(result);
        Assert.Equal(1500, amount);
        Assert.Equal(string.Empty, errorMessage);
    }
}