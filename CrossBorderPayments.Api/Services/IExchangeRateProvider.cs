namespace CrossBorderPayments.Api.Services;

public interface IExchangeRateProvider
{
    Task<DailyExchangeRates> GetLatestRatesAsync(
        CancellationToken cancellationToken = default);

    Task<ExchangeRateResult> GetRateAsync(
        string sourceCurrency,
        string destinationCurrency,
        CancellationToken cancellationToken = default);
}

public sealed record DailyExchangeRates(
    string BaseCurrency,
    IReadOnlyDictionary<string, decimal> Rates,
    DateOnly EffectiveDate,
    string Provider,
    string SourceUrl);

public sealed record ExchangeRateResult(
    decimal Rate,
    DateOnly EffectiveDate,
    string Provider);
