using PocketBudget.Models;
using System.Text.Json;

namespace PocketBudget.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var url = "https://fakestoreapi.com/products/categories";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"La API respondió con el código {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();

        var categoryNames = JsonSerializer.Deserialize<List<string>>(json);

        return categoryNames?
            .Select(name => new Category { Name = FormatCategoryName(name) })
            .ToList() ?? new List<Category>();
    }

    private static string FormatCategoryName(string name)
    {
        return name switch
        {
            "electronics" => "Electrónica",
            "jewelery" => "Accesorios",
            "men's clothing" => "Ropa hombre",
            "women's clothing" => "Ropa mujer",
            _ => name
        };
    }
}