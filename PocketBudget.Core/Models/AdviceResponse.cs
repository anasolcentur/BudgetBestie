using System.Text.Json.Serialization;

namespace PocketBudget.Models;

public class AdviceResponse
{
    [JsonPropertyName("slip")]
    public AdviceSlip Slip { get; set; } = new();
}

public class AdviceSlip
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("advice")]
    public string Advice { get; set; } = string.Empty;
}