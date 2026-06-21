using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Rastreador.Api.Data;
using Rastreador.Api.Models;

namespace Rastreador.Api.Controllers;

[ApiController]
[Route("api/geofences")]
public class GeofencesController : ControllerBase
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    private readonly AppDbContext _db;

    public GeofencesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GeofenceDto>>> GetAll()
    {
        var geofences = await _db.Geofences.ToListAsync();
        return Ok(geofences.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<GeofenceDto>> Create(GeofenceCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Nome é obrigatório.");

        if (dto.Points.Count < 3)
            return BadRequest("É necessário ao menos 3 pontos para formar um polígono.");

        var polygon = BuildPolygon(dto.Points);

        var geofence = new Geofence
        {
            Name = dto.Name.Trim(),
            VehicleId = dto.VehicleId,
            Area = polygon,
            AlertOnEnter = dto.AlertOnEnter,
            AlertOnExit = dto.AlertOnExit
        };

        _db.Geofences.Add(geofence);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), ToDto(geofence));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var geofence = await _db.Geofences.FindAsync(id);
        if (geofence is null) return NotFound();

        _db.Geofences.Remove(geofence);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static Polygon BuildPolygon(List<LatLngDto> points)
    {
        var coordinates = points.Select(p => new Coordinate(p.Lng, p.Lat)).ToList();

        // O anel precisa ser fechado (primeiro ponto == último ponto)
        if (coordinates[0] != coordinates[^1])
            coordinates.Add(coordinates[0]);

        var ring = GeometryFactory.CreateLinearRing(coordinates.ToArray());
        return GeometryFactory.CreatePolygon(ring);
    }

    private static GeofenceDto ToDto(Geofence geofence) => new()
    {
        Id = geofence.Id,
        Name = geofence.Name,
        VehicleId = geofence.VehicleId,
        Points = geofence.Area.Coordinates.Select(c => new LatLngDto { Lat = c.Y, Lng = c.X }).ToList(),
        AlertOnEnter = geofence.AlertOnEnter,
        AlertOnExit = geofence.AlertOnExit,
        CreatedAt = geofence.CreatedAt
    };
}
