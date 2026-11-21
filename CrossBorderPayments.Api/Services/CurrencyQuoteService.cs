using CrossBorderPayments.Api.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CrossBorderPayments.Api.Services;

public class CurrencyQuoteService
{
    private const decimal MaxPaymentAmount = 9_999_999_999_999_999.99m;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IMemoryCache _cache;

    public CurrencyQuoteService(
        IExchangeRateProvider exchangeRateProvider,
        IMemoryCache cache)
    {
        _exchangeRateProvider = exchangeRateProvider;
        _cache = cache;
    }

    public QuoteDto? FindQuote(Guid workspaceId, Guid quoteId) =>
        _cache.Get<QuoteDto>((workspaceId, quoteId));

    public async Task<QuoteDto> CreateQuoteAsync(
        CreateQuoteDto request,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (request.SourceAmount <= 0)
        {
            throw new ArgumentException(
                "Source amount must be greater than zero.");
        }

        if (request.SourceAmount > MaxPaymentAmount ||
            request.SourceAmount != Math.Round(request.SourceAmount, 2))
        {
            throw new ArgumentException(
                "Enter a supported source amount with at most two decimal places.");
        }

        var sourceCurrency = request.SourceCurrency
            .Trim()
            .ToUpperInvariant();

        var destinationCurrency = request.DestinationCurrency
            .Trim()
            .ToUpperInvariant();

        if (sourceCurrency == destinationCurrency)
        {
            throw new ArgumentException(
                "Source and destination currencies must be different.");
        }

        var rateResult = await _exchangeRateProvider.GetRateAsync(
            sourceCurrency,
            destinationCurrency,
            cancellationToken);

        var exchangeRate = rateResult.Rate;

        var fee = Math.Max(
            5m,
            Math.Round(
                request.SourceAmount * 0.015m,
                2,
                MidpointRounding.AwayFromZero));

        var amountAfterFee = request.SourceAmount - fee;

        if (amountAfterFee <= 0)
        {
            throw new ArgumentException(
                "The payment amount must be greater than the transaction fee.");
        }

        var destinationAmount = Math.Round(
            amountAfterFee * exchangeRate,
            2,
            MidpointRounding.AwayFromZero);

        if (destinationAmount <= 0 || destinationAmount > MaxPaymentAmount)
        {
            throw new ArgumentException(
                "The converted amount is outside the supported payment range.");
        }

        var quote = new QuoteDto
        {
            QuoteId = Guid.NewGuid(),
            SourceAmount = request.SourceAmount,
            SourceCurrency = sourceCurrency,
            ExchangeRate = Math.Round(exchangeRate, 6),
            Fee = fee,
            DestinationAmount = destinationAmount,
            DestinationCurrency = destinationCurrency,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            RateProvider = rateResult.Provider,
            RateEffectiveDate = rateResult.EffectiveDate
        };

        _cache.Set((workspaceId, quote.QuoteId), quote, quote.ExpiresAtUtc);
        return quote;
    }
}
