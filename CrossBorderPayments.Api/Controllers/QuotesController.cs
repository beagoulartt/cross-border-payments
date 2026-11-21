using CrossBorderPayments.Api.DTOs;
using CrossBorderPayments.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly CurrencyQuoteService _quoteService;

    public QuotesController(CurrencyQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [HttpPost]
    public async Task<ActionResult<QuoteDto>> CreateQuote(
        CreateQuoteDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quote = await _quoteService.CreateQuoteAsync(
                request,
                User.GetWorkspaceId(),
                cancellationToken);
            return Ok(quote);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = exception.Message
                });
        }
    }
}
