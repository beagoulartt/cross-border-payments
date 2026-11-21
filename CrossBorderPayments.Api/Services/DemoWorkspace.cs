using System.Security.Claims;

namespace CrossBorderPayments.Api.Services;

public static class DemoWorkspace
{
    public const string Claim = "demo_workspace";

    public static Guid GetWorkspaceId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(Claim), out var workspaceId)
            ? workspaceId
            : Guid.Empty;
}
