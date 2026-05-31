using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Abstractions.ExchangeRates;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Nbp;


internal sealed class NbpClient(HttpClient httpClient, ILogger<NbpClient> logger) : INbpClient
{
    private const string TableBUrl = "api/exchangerates/tables/B?format=json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<IReadOnlyList<NbpTableData>>> GetTableBRatesAsync(
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await httpClient.GetAsync(TableBUrl, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request to NBP API failed");
            return Result.Failure<IReadOnlyList<NbpTableData>>(NbpErrors.RequestFailed);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "NBP API request timed out");
            return Result.Failure<IReadOnlyList<NbpTableData>>(NbpErrors.Timeout);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("NBP API returned 404 — no current Table B data available");
            return Result.Success<IReadOnlyList<NbpTableData>>([]);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "NBP API returned unexpected status {StatusCode}",
                (int)response.StatusCode);

            return Result.Failure<IReadOnlyList<NbpTableData>>(
                NbpErrors.UnexpectedStatus((int)response.StatusCode));
        }
        
        string? contentType = response.Content.Headers.ContentType?.MediaType;

        if (contentType is not null && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(
                "NBP API returned HTML instead of JSON (Content-Type: {ContentType})",
                contentType);

            return Result.Failure<IReadOnlyList<NbpTableData>>(NbpErrors.UnexpectedHtmlResponse);
        }

        NbpTableApiResponse[]? dtos;

        try
        {
            dtos = await response.Content.ReadFromJsonAsync<NbpTableApiResponse[]>(
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize NBP API response");
            return Result.Failure<IReadOnlyList<NbpTableData>>(NbpErrors.InvalidJson);
        }

        if (dtos is null || dtos.Length == 0)
        {
            return Result.Success<IReadOnlyList<NbpTableData>>([]);
        }

        IReadOnlyList<NbpTableData> result = dtos
            .Select(MapToTableData)
            .ToList();

        return Result.Success(result);
    }

    private static NbpTableData MapToTableData(NbpTableApiResponse dto)
    {
        IReadOnlyList<NbpRateData> rates = dto.Rates
            .Where(r => !string.IsNullOrWhiteSpace(r.CurrencyCode) && r.MidRate > 0)
            .Select(r => new NbpRateData(r.CurrencyName, r.CurrencyCode, r.MidRate))
            .ToList();

        return new NbpTableData(
            dto.TableNumber,
            DateOnly.Parse(dto.EffectiveDate, CultureInfo.InvariantCulture),
            rates);
    }
}



