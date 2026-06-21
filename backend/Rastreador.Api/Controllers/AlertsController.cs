using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Extensions;
using Rastreador.Api.Models;
using Rastreador.Api.Services;

namespace Rastreador.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PdfReportService _pdfReportService;

    public AlertsController(AppDbContext db, PdfReportService pdfReportService)
    {
        _db = db;
        _pdfReportService = pdfReportService;
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

    [HttpGet("report/pdf")]
    public async Task<IActionResult> GetPdfReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var companyId = User.GetCompanyId();
        var company = await _db.Companies.FindAsync(companyId);
        if (company is null) return NotFound();

        var rangeEnd = to ?? DateTime.UtcNow;
        var rangeStart = from ?? rangeEnd.AddDays(-7);

        var rows = await _db.Alerts
            .Where(a => a.CompanyId == companyId && a.Timestamp >= rangeStart && a.Timestamp <= rangeEnd)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AlertReportRow(a.Vehicle!.Plate, a.Type.ToString(), a.Message, a.Timestamp))
            .ToListAsync();

        var pdfBytes = _pdfReportService.GenerateAlertsReport(company.Name, rangeStart, rangeEnd, rows);
        return File(pdfBytes, "application/pdf", $"alertas-{rangeStart:yyyyMMdd}-{rangeEnd:yyyyMMdd}.pdf");
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
