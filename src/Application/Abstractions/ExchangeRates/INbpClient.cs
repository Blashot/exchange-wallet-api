using SharedKernel;

namespace Application.Abstractions.ExchangeRates;


public interface INbpClient
{
    Task<Result<IReadOnlyList<NbpTableData>>> GetTableBRatesAsync(CancellationToken cancellationToken);
}

