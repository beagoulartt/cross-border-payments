using CrossBorderPayments.Api.Data;
using CrossBorderPayments.Api.DTOs;
using CrossBorderPayments.Api.Models;
using CrossBorderPayments.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrencyQuoteService _quoteService;

    public PaymentsController(
        AppDbContext context,
        CurrencyQuoteService quoteService)
    {
        _context = context;
        _quoteService = quoteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments()
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Beneficiary)
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .ToListAsync();

        return Ok(payments.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(int id)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Beneficiary)
            .FirstOrDefaultAsync(payment => payment.Id == id);

        if (payment is null)
        {
            return NotFound(new
            {
                message = $"Payment with ID {id} was not found."
            });
        }

        return Ok(ToDto(payment));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> CreatePayment(
        CreatePaymentDto request)
    {
        var beneficiary = await _context.Beneficiaries
            .FirstOrDefaultAsync(
                beneficiary => beneficiary.Id == request.BeneficiaryId);

        if (beneficiary is null)
        {
            return NotFound(new
            {
                message =
                    $"Beneficiary with ID {request.BeneficiaryId} was not found."
            });
        }

        if (!beneficiary.IsActive)
        {
            return BadRequest(new
            {
                message = "Payments cannot be sent to an inactive beneficiary."
            });
        }

        QuoteDto quote;

        try
        {
            quote = _quoteService.CreateQuote(new CreateQuoteDto
            {
                SourceAmount = request.SourceAmount,
                SourceCurrency = request.SourceCurrency,
                DestinationCurrency = request.DestinationCurrency
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }

        var payment = new Payment
        {
            BeneficiaryId = beneficiary.Id,
            Beneficiary = beneficiary,
            SourceAmount = quote.SourceAmount,
            SourceCurrency = quote.SourceCurrency,
            DestinationAmount = quote.DestinationAmount,
            DestinationCurrency = quote.DestinationCurrency,
            ExchangeRate = quote.ExchangeRate,
            Fee = quote.Fee,
            Status = PaymentStatus.Submitted
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPayment),
            new { id = payment.Id },
            ToDto(payment));
    }


[HttpPatch("{id:int}/status")]
public async Task<ActionResult<PaymentDto>> UpdatePaymentStatus(
    int id,
    UpdatePaymentStatusDto request)
{
    var payment = await _context.Payments
        .Include(payment => payment.Beneficiary)
        .FirstOrDefaultAsync(payment => payment.Id == id);

    if (payment is null)
    {
        return NotFound(new
        {
            message = $"Payment with ID {id} was not found."
        });
    }

    var newStatus = request.Status!.Value;

    if (!IsValidStatusTransition(payment.Status, newStatus))
    {
        return BadRequest(new
        {
            message =
                $"Payment cannot move from {payment.Status} to {newStatus}."
        });
    }

    payment.Status = newStatus;

    await _context.SaveChangesAsync();

    return Ok(ToDto(payment));
}

private static bool IsValidStatusTransition(
    PaymentStatus currentStatus,
    PaymentStatus newStatus)
{
    return currentStatus switch
    {
        PaymentStatus.Draft =>
            newStatus == PaymentStatus.Submitted,

        PaymentStatus.Submitted =>
            newStatus is PaymentStatus.PendingApproval
                or PaymentStatus.Rejected,

        PaymentStatus.PendingApproval =>
            newStatus is PaymentStatus.Processing
                or PaymentStatus.Rejected,

        PaymentStatus.Processing =>
            newStatus is PaymentStatus.Completed
                or PaymentStatus.Failed,

        PaymentStatus.Completed => false,
        PaymentStatus.Rejected => false,
        PaymentStatus.Failed => false,

        _ => false
    };
}


    private static PaymentDto ToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            BeneficiaryId = payment.BeneficiaryId,
            BeneficiaryName = payment.Beneficiary?.Name ?? string.Empty,
            SourceAmount = payment.SourceAmount,
            SourceCurrency = payment.SourceCurrency,
            DestinationAmount = payment.DestinationAmount,
            DestinationCurrency = payment.DestinationCurrency,
            ExchangeRate = payment.ExchangeRate,
            Fee = payment.Fee,
            Status = payment.Status,
            CreatedAtUtc = payment.CreatedAtUtc
        };
    }
}