using System.Text.Json.Serialization;

namespace Infrastructure.Nbp;

internal sealed class NbpTableApiResponse
{
    [JsonPropertyName("table")]
    public string Table { get; init; } = string.Empty;

    [JsonPropertyName("no")]
    public string TableNumber { get; init; } = string.Empty;

    [JsonPropertyName("effectiveDate")]
    public string EffectiveDate { get; init; } = string.Empty;

    [JsonPropertyName("rates")]
    public List<NbpRateApiResponse> Rates { get; init; } = [];
}

internal sealed class NbpRateApiResponse
{
    [JsonPropertyName("currency")]
    public string CurrencyName { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string CurrencyCode { get; init; } = string.Empty;

    [JsonPropertyName("mid")]
    public decimal MidRate { get; init; }
}

