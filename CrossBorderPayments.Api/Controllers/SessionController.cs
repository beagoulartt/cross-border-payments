using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CrossBorderPayments.Api.Data;
using CrossBorderPayments.Api.Models;
using CrossBorderPayments.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionController(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    AppDbContext database) : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetSession()
    {
        var session = User.Identity?.IsAuthenticated == true
            ? new DemoSession(User.Identity.Name!, User.FindFirstValue(ClaimTypes.Role)!,
                User.GetWorkspaceId() != Guid.Empty)
            : null;

        return Ok(new { session, portfolioEnabled = configuration.GetValue<bool>("Portfolio:Enabled") });
    }

    [HttpPost("login")]
    [Consumes("application/json")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var session = (request.Username.Trim().ToLowerInvariant(), request.Password) switch
        {
            ("admin", "AdminDemo123!") => new DemoSession("admin", "Administrator"),
            ("usuario", "UsuarioDemo123!") => new DemoSession("usuario", "User"),
            _ => null
        };

        if (session is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        await SignInAsync(session, Guid.Empty);
        return Ok(session);
    }

    [HttpPost("demo")]
    [Consumes("application/json")]
    public async Task<IActionResult> EnterDemo(DemoRequest request, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Portfolio:Enabled"))
        {
            return NotFound();
        }

        var session = request.Role switch
        {
            "User" => new DemoSession("Demo user", "User", true),
            "Administrator" => new DemoSession("Demo administrator", "Administrator", true),
            _ => null
        };

        if (session is null)
        {
            return BadRequest(new { message = "Choose a demo role." });
        }

        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == Guid.Empty)
        {
            workspaceId = Guid.NewGuid();
            database.Beneficiaries.AddRange(
                new Beneficiary
                {
                    WorkspaceId = workspaceId, Name = "São Paulo Design Studio", Country = "Brazil",
                    Currency = "BRL", BankName = "Demo Bank", AccountNumber = "00001234"
                },
                new Beneficiary
                {
                    WorkspaceId = workspaceId, Name = "Brooklyn Print Co.", Country = "United States",
                    Currency = "USD", BankName = "Demo Bank", AccountNumber = "00005678"
                });
            await database.SaveChangesAsync(cancellationToken);
        }

        await SignInAsync(session, workspaceId);
        return Ok(session);
    }

    private Task SignInAsync(DemoSession session, Guid workspaceId)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, session.Username),
                new Claim(ClaimTypes.Role, session.Role),
                new Claim(DemoWorkspace.Claim, workspaceId.ToString())
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return HttpContext.SignInAsync(new ClaimsPrincipal(identity));
    }

    [HttpPost("logout")]
    [Consumes("application/json")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    public sealed record LoginRequest([Required] string Username, [Required] string Password);
    public sealed record DemoRequest([Required] string Role);

    private sealed record DemoSession(string Username, string Role, bool IsDemo = false);
}
