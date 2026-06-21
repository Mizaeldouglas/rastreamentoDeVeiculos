using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Extensions;
using Rastreador.Api.Models;

namespace Rastreador.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AlertsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAll([FromQuery] int? vehicleId)
    {
        var companyId = User.GetCompanyId();
        var query = _db.Alerts.Where(a => a.CompanyId == companyId);
        if (vehicleId is not null)
            query = query.Where(a => a.VehicleId == vehicleId);

        var alerts = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(200)
            .Select(a => new AlertDto
            {
                Id = a.Id,
                VehicleId = a.VehicleId,
                Type = a.Type.ToString(),
                Message = a.Message,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Timestamp = a.Timestamp,
                Acknowledged = a.Acknowledged
            })
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpPost("{id:int}/ack")]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var companyId = User.GetCompanyId();
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId);
        if (alert is null) return NotFound();

        alert.Acknowledged = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
