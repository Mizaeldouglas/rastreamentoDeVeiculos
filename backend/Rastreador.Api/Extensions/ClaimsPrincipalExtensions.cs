using System.Security.Claims;

namespace Rastreador.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCompanyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("companyId");
        return value is null ? 0 : int.Parse(value);
    }
}
