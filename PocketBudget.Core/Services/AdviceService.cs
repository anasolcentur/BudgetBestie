using PocketBudget.Models;
using System.Text.Json;

namespace PocketBudget.Services;

public class AdviceService : IAdviceService
{
    private readonly HttpClient _httpClient;

    public AdviceService() : this(new HttpClient())
    {
    }

    public AdviceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRandomAdviceAsync()
    {
        var url = "https://api.adviceslip.com/advice";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"La API respondió con el código {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();

        var adviceResponse = JsonSerializer.Deserialize<AdviceResponse>(json);

        if (adviceResponse?.Slip is null || string.IsNullOrWhiteSpace(adviceResponse.Slip.Advice))
        {
            throw new JsonException("La respuesta de la API no contiene un consejo válido.");
        }

        return adviceResponse.Slip.Advice;
    }
}