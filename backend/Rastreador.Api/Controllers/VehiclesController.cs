using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Extensions;
using Rastreador.Api.Models;

namespace Rastreador.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _db;

    public VehiclesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
    {
        var companyId = User.GetCompanyId();

        var vehicles = await _db.Vehicles
            .Where(v => v.CompanyId == companyId)
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Plate = v.Plate,
                Model = v.Model,
                Driver = v.Driver,
                Imei = v.Imei,
                SpeedLimitKmh = v.SpeedLimitKmh,
                CreatedAt = v.CreatedAt,
                LastPosition = v.Positions
                    .OrderByDescending(p => p.Timestamp)
                    .Select(p => new PositionDto
                    {
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Speed = p.Speed,
                        Heading = p.Heading,
                        Timestamp = p.Timestamp
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleDto>> GetById(int id)
    {
        var companyId = User.GetCompanyId();
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
        if (vehicle is null) return NotFound();

        var lastPosition = await _db.Positions
            .Where(p => p.VehicleId == id)
            .OrderByDescending(p => p.Timestamp)
            .Select(p => new PositionDto
            {
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Speed = p.Speed,
                Heading = p.Heading,
                Timestamp = p.Timestamp
            })
            .FirstOrDefaultAsync();

        return Ok(new VehicleDto
        {
            Id = vehicle.Id,
            Plate = vehicle.Plate,
            Model = vehicle.Model,
            Driver = vehicle.Driver,
            Imei = vehicle.Imei,
            SpeedLimitKmh = vehicle.SpeedLimitKmh,
            CreatedAt = vehicle.CreatedAt,
            LastPosition = lastPosition
        });
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetHistory(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var companyId = User.GetCompanyId();
        var exists = await _db.Vehicles.AnyAsync(v => v.Id == id && v.CompanyId == companyId);
        if (!exists) return NotFound();

        var rangeEnd = to ?? DateTime.UtcNow;
        var rangeStart = from ?? rangeEnd.AddHours(-24);

        var history = await _db.Positions
            .Where(p => p.VehicleId == id && p.Timestamp >= rangeStart && p.Timestamp <= rangeEnd)
            .OrderBy(p => p.Timestamp)
            .Take(5000)
            .Select(p => new PositionDto
            {
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Speed = p.Speed,
                Heading = p.Heading,
                Timestamp = p.Timestamp
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(VehicleCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Plate))
            return BadRequest("Placa é obrigatória.");

        var vehicle = new Vehicle
        {
            CompanyId = User.GetCompanyId(),
            Plate = dto.Plate.Trim().ToUpperInvariant(),
            Model = dto.Model.Trim(),
            Driver = dto.Driver.Trim(),
            Imei = string.IsNullOrWhiteSpace(dto.Imei) ? null : dto.Imei.Trim(),
            SpeedLimitKmh = dto.SpeedLimitKmh
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        var result = new VehicleDto
        {
            Id = vehicle.Id,
            Plate = vehicle.Plate,
            Model = vehicle.Model,
            Driver = vehicle.Driver,
            Imei = vehicle.Imei,
            SpeedLimitKmh = vehicle.SpeedLimitKmh,
            CreatedAt = vehicle.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VehicleCreateDto dto)
    {
        var companyId = User.GetCompanyId();
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
        if (vehicle is null) return NotFound();

        vehicle.Plate = dto.Plate.Trim().ToUpperInvariant();
        vehicle.Model = dto.Model.Trim();
        vehicle.Driver = dto.Driver.Trim();
        vehicle.Imei = string.IsNullOrWhiteSpace(dto.Imei) ? null : dto.Imei.Trim();
        vehicle.SpeedLimitKmh = dto.SpeedLimitKmh;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var companyId = User.GetCompanyId();
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId);
        if (vehicle is null) return NotFound();

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
