using System.Security.Claims;
using Rastreador.Api.Extensions;
using Xunit;

namespace Rastreador.Api.Tests;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetCompanyId_WithClaimPresent_ReturnsParsedValue()
    {
        var identity = new ClaimsIdentity([new Claim("companyId", "42")]);
        var user = new ClaimsPrincipal(identity);

        Assert.Equal(42, user.GetCompanyId());
    }

    [Fact]
    public void GetCompanyId_WithoutClaim_ReturnsZero()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Equal(0, user.GetCompanyId());
    }
}
