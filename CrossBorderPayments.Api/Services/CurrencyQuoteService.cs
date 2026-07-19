using CrossBorderPayments.Api.DTOs;

namespace CrossBorderPayments.Api.Services;

public class CurrencyQuoteService
{
    private static readonly Dictionary<string, decimal> CurrencyRates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CAD"] = 1.00m,
            ["USD"] = 0.73m,
            ["EUR"] = 0.67m,
            ["GBP"] = 0.56m,
            ["BRL"] = 4.07m
        };

    public QuoteDto CreateQuote(CreateQuoteDto request)
    {
        if (request.SourceAmount <= 0)
        {
            throw new ArgumentException(
                "Source amount must be greater than zero.");
        }

        var sourceCurrency = request.SourceCurrency
            .Trim()
            .ToUpperInvariant();

        var destinationCurrency = request.DestinationCurrency
            .Trim()
            .ToUpperInvariant();

        if (!CurrencyRates.TryGetValue(
                sourceCurrency,
                out var sourceRate))
        {
            throw new ArgumentException(
                $"Currency {sourceCurrency} is not supported.");
        }

        if (!CurrencyRates.TryGetValue(
                destinationCurrency,
                out var destinationRate))
        {
            throw new ArgumentException(
                $"Currency {destinationCurrency} is not supported.");
        }

        if (sourceCurrency == destinationCurrency)
        {
            throw new ArgumentException(
                "Source and destination currencies must be different.");
        }

        var exchangeRate = destinationRate / sourceRate;

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

        return new QuoteDto
        {
            QuoteId = Guid.NewGuid(),
            SourceAmount = request.SourceAmount,
            SourceCurrency = sourceCurrency,
            ExchangeRate = Math.Round(exchangeRate, 6),
            Fee = fee,
            DestinationAmount = destinationAmount,
            DestinationCurrency = destinationCurrency,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };
    }
}