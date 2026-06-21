using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Models;

namespace Rastreador.Api.Controllers;

[ApiController]
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
        var query = _db.Alerts.AsQueryable();
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
        var alert = await _db.Alerts.FindAsync(id);
        if (alert is null) return NotFound();

        alert.Acknowledged = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
