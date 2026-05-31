using SharedKernel;

namespace Domain.ExchangeRates.Events;

public sealed record ExchangeRateTableImportedDomainEvent(
    Guid ExchangeRateTableId,
    DateOnly PublicationDate) : IDomainEvent;

