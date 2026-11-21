using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace CrossBorderPayments.Api.Services;

public class BankOfCanadaExchangeRateProvider : IExchangeRateProvider
{
    private const string RatesCacheKey = "bank-of-canada-daily-rates";
    private const string ProviderName = "Bank of Canada";
    private const string SourceUrl = "https://www.bankofcanada.ca/rates/exchange/daily-exchange-rates/";

    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CAD",
            "USD",
            "EUR",
            "GBP",
            "BRL"
        };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BankOfCanadaExchangeRateProvider> _logger;

    public BankOfCanadaExchangeRateProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<BankOfCanadaExchangeRateProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExchangeRateResult> GetRateAsync(
        string sourceCurrency,
        string destinationCurrency,
        CancellationToken cancellationToken = default)
    {
        var source = NormalizeCurrency(sourceCurrency);
        var destination = NormalizeCurrency(destinationCurrency);

        if (source == destination)
        {
            throw new ArgumentException(
                "Source and destination currencies must be different.");
        }

        var dailyRates = await GetDailyRatesAsync(cancellationToken);

        var sourceCadRate = GetCadRate(dailyRates, source);
        var destinationCadRate = GetCadRate(dailyRates, destination);
        var rate = sourceCadRate / destinationCadRate;

        return new ExchangeRateResult(
            rate,
            dailyRates.EffectiveDate,
            ProviderName);
    }

    public async Task<DailyExchangeRates> GetLatestRatesAsync(
        CancellationToken cancellationToken = default)
    {
        var dailyRates = await GetDailyRatesAsync(cancellationToken);
        return new DailyExchangeRates(
            "CAD",
            dailyRates.CadPerCurrency.ToDictionary(
                entry => entry.Key,
                entry => 1m / entry.Value),
            dailyRates.EffectiveDate,
            ProviderName,
            SourceUrl);
    }

    private async Task<DailyRates> GetDailyRatesAsync(
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue<DailyRates>(
                RatesCacheKey,
                out var cachedRates) &&
            cachedRates is not null)
        {
            return cachedRates;
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                "observations/group/FX_RATES_DAILY/json?recent=10",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var responseStream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: cancellationToken);

            var rates = ParseRates(document.RootElement);

            _cache.Set(
                RatesCacheKey,
                rates,
                TimeSpan.FromMinutes(15));

            return rates;
        }
        catch (Exception exception)
            when (exception is HttpRequestException or JsonException ||
                exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Unable to retrieve exchange rates from the Bank of Canada.");

            throw new InvalidOperationException(
                "Live exchange rates are temporarily unavailable.",
                exception);
        }
    }

    private static DailyRates ParseRates(JsonElement root)
    {
        var requiredSeries = SupportedCurrencies
            .Where(currency => currency != "CAD")
            .ToDictionary(
                currency => $"FX{currency}CAD",
                currency => currency,
                StringComparer.OrdinalIgnoreCase);

        var observations = root
            .GetProperty("observations")
            .EnumerateArray()
            .OrderByDescending(observation =>
                observation.GetProperty("d").GetString());

        foreach (var observation in observations)
        {
            var parsedRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var observationDate = DateOnly.ParseExact(
                observation.GetProperty("d").GetString()!,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

            foreach (var series in requiredSeries)
            {
                if (!observation.TryGetProperty(
                        series.Key,
                        out var rateElement))
                {
                    continue;
                }

                var value = rateElement.GetProperty("v").GetString();

                if (decimal.TryParse(
                        value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var rate) && rate > 0)
                {
                    parsedRates[series.Value] = rate;
                }
            }

            if (parsedRates.Count == requiredSeries.Count)
            {
                return new DailyRates(parsedRates, observationDate);
            }
        }

        throw new JsonException(
            "The Bank of Canada response did not contain a date with all required rates.");
    }

    private static decimal GetCadRate(
        DailyRates rates,
        string currency)
    {
        return currency == "CAD"
            ? 1m
            : rates.CadPerCurrency[currency];
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (!SupportedCurrencies.Contains(normalizedCurrency))
        {
            throw new ArgumentException(
                $"Currency {normalizedCurrency} is not supported.");
        }

        return normalizedCurrency;
    }

    private sealed record DailyRates(
        IReadOnlyDictionary<string, decimal> CadPerCurrency,
        DateOnly EffectiveDate);
}
