using CrossBorderPayments.Api.Data;
using CrossBorderPayments.Api.DTOs;
using CrossBorderPayments.Api.Models;
using CrossBorderPayments.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator,User")]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrencyQuoteService _quoteService;

    private IQueryable<Payment> WorkspacePayments
    {
        get
        {
            var workspaceId = User.GetWorkspaceId();
            return _context.Payments.Where(payment => payment.WorkspaceId == workspaceId);
        }
    }

    public PaymentsController(
        AppDbContext context,
        CurrencyQuoteService quoteService)
    {
        _context = context;
        _quoteService = quoteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments(CancellationToken cancellationToken)
    {
        await CompleteDuePayments(cancellationToken);
        var payments = await WorkspacePayments
            .AsNoTracking()
            .Include(payment => payment.Beneficiary)
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(payments.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(int id, CancellationToken cancellationToken)
    {
        await CompleteDuePayments(cancellationToken);
        var payment = await WorkspacePayments
            .AsNoTracking()
            .Include(payment => payment.Beneficiary)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

        if (payment is null)
        {
            return NotFound(new
            {
                message = $"Payment with ID {id} was not found."
            });
        }

        return Ok(ToDto(payment));
    }

    private async Task CompleteDuePayments(CancellationToken cancellationToken)
    {
        if (User.GetWorkspaceId() == Guid.Empty)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await WorkspacePayments
            .Where(payment => payment.Status == PaymentStatus.Processing &&
                payment.SimulatedCompletionAtUtc <= now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(payment => payment.Status, PaymentStatus.Completed)
                .SetProperty(payment => payment.SimulatedCompletionAtUtc, (DateTime?)null),
                cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<ActionResult<PaymentDto>> CreatePayment(
        CreatePaymentDto request,
        CancellationToken cancellationToken)
    {
        var quoteId = request.QuoteId!.Value;
        if (quoteId == Guid.Empty)
        {
            return BadRequest(new { message = "A valid quote ID is required." });
        }

        var existingPayment = await FindPaymentByQuoteAsync(quoteId, cancellationToken);
        if (existingPayment is not null)
        {
            return ExistingPaymentResult(existingPayment, request.BeneficiaryId);
        }

        var workspaceId = User.GetWorkspaceId();
        var quote = _quoteService.FindQuote(workspaceId, quoteId);
        if (quote is null)
        {
            return QuoteUnavailable();
        }

        var beneficiary = await _context.Beneficiaries
            .FirstOrDefaultAsync(
                beneficiary => beneficiary.Id == request.BeneficiaryId &&
                    beneficiary.WorkspaceId == workspaceId, cancellationToken);

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

        if (!string.Equals(
                beneficiary.Currency,
                quote.DestinationCurrency,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    $"The quote destination currency must match the beneficiary currency ({beneficiary.Currency})."
            });
        }

        if (quote.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return QuoteUnavailable();
        }

        var payment = new Payment
        {
            WorkspaceId = workspaceId,
            QuoteId = quoteId,
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
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent submission may have already used this quote.
            existingPayment = await FindPaymentByQuoteAsync(quoteId, cancellationToken);
            if (existingPayment is null)
            {
                throw;
            }

            return ExistingPaymentResult(existingPayment, request.BeneficiaryId);
        }

        return CreatedAtAction(
            nameof(GetPayment),
            new { id = payment.Id },
            ToDto(payment));
    }


    private Task<Payment?> FindPaymentByQuoteAsync(
        Guid quoteId,
        CancellationToken cancellationToken) =>
        WorkspacePayments.AsNoTracking()
            .Include(payment => payment.Beneficiary)
            .FirstOrDefaultAsync(payment => payment.QuoteId == quoteId, cancellationToken);

    private ActionResult<PaymentDto> ExistingPaymentResult(Payment payment, int beneficiaryId)
    {
        if (payment.BeneficiaryId != beneficiaryId)
        {
            return Conflict(new { message = "This quote has already been used for another beneficiary." });
        }

        return Ok(ToDto(payment));
    }

    private ObjectResult QuoteUnavailable() =>
        StatusCode(StatusCodes.Status410Gone, new
        {
            message = "This quote has expired or is unavailable. Request a new quote."
        });

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<PaymentDto>> UpdatePaymentStatus(
        int id, UpdatePaymentStatusDto request, CancellationToken cancellationToken)
    {
        var payment = await WorkspacePayments.AsNoTracking()
            .Include(payment => payment.Beneficiary)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

        if (payment is null)
        {
            return NotFound(new { message = $"Payment with ID {id} was not found." });
        }

        var previousStatus = payment.Status;
        var newStatus = request.Status!.Value;
        var isDemo = payment.WorkspaceId != Guid.Empty;
        if (!IsValidStatusTransition(previousStatus, newStatus) ||
            (isDemo && previousStatus == PaymentStatus.Processing))
        {
            return BadRequest(new
            {
                message = $"Payment cannot move from {previousStatus} to {newStatus}."
            });
        }

        DateTime? completionAt = isDemo && newStatus == PaymentStatus.Processing
            ? DateTime.UtcNow.AddSeconds(8)
            : null;

        var changed = await WorkspacePayments
            .Where(candidate => candidate.Id == id && candidate.Status == previousStatus)
            .ExecuteUpdateAsync(update => update
                .SetProperty(candidate => candidate.Status, newStatus)
                .SetProperty(candidate => candidate.SimulatedCompletionAtUtc, completionAt),
                cancellationToken);
        if (changed == 0)
        {
            return Conflict(new { message = "The payment status changed. Refresh and try again." });
        }

        payment.Status = newStatus;
        return Ok(ToDto(payment));
    }

    private static bool IsValidStatusTransition(PaymentStatus currentStatus, PaymentStatus newStatus) =>
        currentStatus switch
        {
            PaymentStatus.Draft => newStatus == PaymentStatus.Submitted,
            PaymentStatus.Submitted => newStatus is PaymentStatus.PendingApproval or PaymentStatus.Rejected,
            PaymentStatus.PendingApproval => newStatus is PaymentStatus.Processing or PaymentStatus.Rejected,
            PaymentStatus.Processing => newStatus is PaymentStatus.Completed or PaymentStatus.Failed,
            _ => false
        };

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
