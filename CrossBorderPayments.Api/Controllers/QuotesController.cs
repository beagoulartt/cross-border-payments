using CrossBorderPayments.Api.DTOs;
using CrossBorderPayments.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly CurrencyQuoteService _quoteService;

    public QuotesController(CurrencyQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [HttpPost]
    public ActionResult<QuoteDto> CreateQuote(CreateQuoteDto request)
    {
        try
        {
            var quote = _quoteService.CreateQuote(request);
            return Ok(quote);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}