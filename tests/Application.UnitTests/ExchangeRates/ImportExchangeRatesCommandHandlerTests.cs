using Application.Abstractions.ExchangeRates;
using Application.ExchangeRates.Commands.ImportExchangeRates;
using Domain.ExchangeRates;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace Application.UnitTests.ExchangeRates;

public sealed class ImportExchangeRatesCommandHandlerTests
{
    private readonly INbpClient _nbpClient = Substitute.For<INbpClient>();
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTime Now = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ImportExchangeRatesCommand Command = new();

    public ImportExchangeRatesCommandHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private ImportExchangeRatesCommandHandler CreateHandler() =>
        new(_nbpClient, _context, _clock);

    private static NbpTableData BuildTable(
        string tableNumber,
        DateOnly date,
        params (string Code, string Name, decimal Rate)[] rates) =>
        new(tableNumber, date, rates.Select(r => new NbpRateData(r.Name, r.Code, r.Rate)).ToList());

    private void SetupExistingTables(params ExchangeRateTable[] tables)
    {
        DbSet<ExchangeRateTable> mockDbSet = tables.AsQueryable().BuildMockDbSet();
        _context.ExchangeRateTables.Returns(mockDbSet);
    }

    
    [Fact]
    public async Task Handle_NewTable_ShouldReturnSuccessWithCounts()
    {
        NbpTableData tableData = BuildTable(
            "001/B/NBP/2026",
            new DateOnly(2026, 5, 29),
            ("USD", "US Dollar", 4.00m),
            ("EUR", "Euro", 4.50m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([tableData]));

        SetupExistingTables();

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TablesImported.ShouldBe(1);
        result.Value.RatesImported.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_NewTable_ShouldAddTableToContext()
    {
        NbpTableData tableData = BuildTable(
            "001/B/NBP/2026",
            new DateOnly(2026, 5, 29),
            ("USD", "US Dollar", 4.00m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([tableData]));

        SetupExistingTables();
        DbSet<ExchangeRateTable> tableDbSet = _context.ExchangeRateTables;

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        tableDbSet.Received(1).Add(Arg.Any<ExchangeRateTable>());
    }

    [Fact]
    public async Task Handle_NewTable_ShouldPersistChanges()
    {
        NbpTableData tableData = BuildTable(
            "001/B/NBP/2026",
            new DateOnly(2026, 5, 29),
            ("USD", "US Dollar", 4.00m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([tableData]));

        SetupExistingTables();

        await CreateHandler().Handle(Command, CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    
    [Fact]
    public async Task Handle_TableAlreadyImported_ShouldSkipAndReturnZeroCounts()
    {
        const string tableNumber = "001/B/NBP/2026";
        NbpTableData tableData = BuildTable(
            tableNumber,
            new DateOnly(2026, 5, 29),
            ("USD", "US Dollar", 4.00m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([tableData]));

       
        var existing = ExchangeRateTable.Create(
            tableNumber,
            new DateOnly(2026, 5, 29),
            Now,
            [(new CurrencyCode("USD"), "US Dollar", 4.00m)]);
        SetupExistingTables(existing);

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TablesImported.ShouldBe(0);
        result.Value.RatesImported.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_TableAlreadyImported_ShouldNotCallSaveChanges()
    {
        const string tableNumber = "001/B/NBP/2026";
        NbpTableData tableData = BuildTable(
            tableNumber,
            new DateOnly(2026, 5, 29),
            ("USD", "US Dollar", 4.00m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([tableData]));

        var existing = ExchangeRateTable.Create(
            tableNumber,
            new DateOnly(2026, 5, 29),
            Now,
            [(new CurrencyCode("USD"), "US Dollar", 4.00m)]);
        SetupExistingTables(existing);

        await CreateHandler().Handle(Command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Handle_MixedBatch_ShouldImportOnlyNewTables()
    {
        const string existingNumber = "001/B/NBP/2026";
        const string newNumber = "002/B/NBP/2026";

        NbpTableData existingData = BuildTable(existingNumber, new DateOnly(2026, 5, 1), ("USD", "US Dollar", 4.00m));
        NbpTableData newData = BuildTable(newNumber, new DateOnly(2026, 5, 8), ("USD", "US Dollar", 4.01m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([existingData, newData]));

        var existing = ExchangeRateTable.Create(
            existingNumber, new DateOnly(2026, 5, 1), Now,
            [(new CurrencyCode("USD"), "US Dollar", 4.00m)]);
        SetupExistingTables(existing);

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TablesImported.ShouldBe(1);
        result.Value.RatesImported.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_EmptyNbpResponse_ShouldReturnSuccessWithZeroCounts()
    {
        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([]));

        SetupExistingTables();

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TablesImported.ShouldBe(0);
        result.Value.RatesImported.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_EmptyNbpResponse_ShouldNotCallSaveChanges()
    {
        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([]));

        SetupExistingTables();

        await CreateHandler().Handle(Command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Handle_NbpClientFailure_ShouldReturnFailure()
    {
        var networkError = Error.Problem("Nbp.NetworkError", "Failed to reach NBP API.");
        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<NbpTableData>>(networkError));

        SetupExistingTables();

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Nbp.NetworkError");
    }

    [Fact]
    public async Task Handle_NbpClientFailure_ShouldNotCallSaveChanges()
    {
        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<NbpTableData>>(
                Error.Problem("Nbp.NetworkError", "Network failure.")));

        SetupExistingTables();

        await CreateHandler().Handle(Command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
    
    [Fact]
    public async Task Handle_MultipleTables_ShouldImportAllAndCountCorrectly()
    {
        NbpTableData t1 = BuildTable("001/B/NBP/2026", new DateOnly(2026, 5, 1),
            ("USD", "US Dollar", 4.00m), ("EUR", "Euro", 4.50m));

        NbpTableData t2 = BuildTable("002/B/NBP/2026", new DateOnly(2026, 5, 8),
            ("USD", "US Dollar", 4.02m), ("EUR", "Euro", 4.52m), ("GBP", "Pound Sterling", 5.10m));

        _nbpClient.GetTableBRatesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<NbpTableData>>([t1, t2]));

        SetupExistingTables();

        Result<ImportExchangeRatesResult> result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TablesImported.ShouldBe(2);
        result.Value.RatesImported.ShouldBe(5); // 2 + 3
    }
}


