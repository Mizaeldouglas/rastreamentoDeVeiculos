using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Extensions;
using Rastreador.Api.Models;

namespace Rastreador.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/push")]
public class PushController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public PushController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpGet("vapid-public-key")]
    public ActionResult<string> GetVapidPublicKey()
    {
        return Ok(_configuration["Vapid:PublicKey"]);
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(PushSubscriptionRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint))
            return BadRequest("Endpoint é obrigatório.");

        var companyId = User.GetCompanyId();

        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(p => p.Endpoint == dto.Endpoint);
        if (existing is not null)
        {
            existing.CompanyId = companyId;
            existing.P256dh = dto.Keys.P256dh;
            existing.Auth = dto.Keys.Auth;
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                CompanyId = companyId,
                Endpoint = dto.Endpoint,
                P256dh = dto.Keys.P256dh,
                Auth = dto.Keys.Auth
            });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("subscribe")]
    public async Task<IActionResult> Unsubscribe(PushSubscriptionRequestDto dto)
    {
        var companyId = User.GetCompanyId();
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(p => p.Endpoint == dto.Endpoint && p.CompanyId == companyId);
        if (existing is null) return NotFound();

        _db.PushSubscriptions.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
