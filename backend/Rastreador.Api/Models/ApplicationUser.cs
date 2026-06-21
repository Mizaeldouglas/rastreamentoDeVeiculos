using Microsoft.AspNetCore.Identity;

namespace Rastreador.Api.Models;

public class ApplicationUser : IdentityUser<int>
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}
