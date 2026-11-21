using CrossBorderPayments.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRatesController(IExchangeRateProvider provider) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<DailyExchangeRates>> GetLatest(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await provider.GetLatestRatesAsync(cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }
    }
}
