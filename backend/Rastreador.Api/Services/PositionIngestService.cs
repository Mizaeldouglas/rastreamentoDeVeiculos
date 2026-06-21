using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Rastreador.Api.Data;
using Rastreador.Api.Hubs;
using Rastreador.Api.Models;
using Position = Rastreador.Api.Models.Position;

namespace Rastreador.Api.Services;

/// <summary>
/// Ponto único de entrada para qualquer posição de GPS recebida (simulada ou real via GT06):
/// persiste, transmite via SignalR e roda o motor de eventos (geofence + limite de velocidade).
/// </summary>
public class PositionIngestService
{
    private readonly IHubContext<PositionHub> _hubContext;
    private readonly ILogger<PositionIngestService> _logger;
    private readonly double _defaultSpeedLimitKmh;
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public PositionIngestService(
        IHubContext<PositionHub> hubContext,
        ILogger<PositionIngestService> logger,
        IConfiguration configuration)
    {
        _hubContext = hubContext;
        _logger = logger;
        _defaultSpeedLimitKmh = configuration.GetValue("Alerts:DefaultSpeedLimitKmh", 100.0);
    }

    public async Task IngestAsync(
        AppDbContext db,
        int vehicleId,
        double latitude,
        double longitude,
        double speed,
        double heading,
        DateTime timestampUtc,
        CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FindAsync([vehicleId], cancellationToken);
        if (vehicle is null) return;

        var previousPosition = await db.Positions
            .Where(p => p.VehicleId == vehicleId)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var position = new Position
        {
            VehicleId = vehicleId,
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Heading = heading,
            Timestamp = timestampUtc
        };

        db.Positions.Add(position);
        await db.SaveChangesAsync(cancellationToken);

        var companyGroup = PositionHub.CompanyGroup(vehicle.CompanyId);

        await _hubContext.Clients.Group(companyGroup).SendAsync("PositionUpdated", new
        {
            vehicleId = position.VehicleId,
            latitude = position.Latitude,
            longitude = position.Longitude,
            speed = position.Speed,
            heading = position.Heading,
            timestamp = position.Timestamp
        }, cancellationToken);

        await EvaluateEventsAsync(db, vehicle, previousPosition, position, companyGroup, cancellationToken);
    }

    public async Task IngestIgnitionAsync(
        AppDbContext db,
        int vehicleId,
        bool ignitionOn,
        DateTime timestampUtc,
        CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FindAsync([vehicleId], cancellationToken);
        if (vehicle is null) return;

        if (vehicle.IgnitionOn == ignitionOn) return; // sem mudança de estado — não duplica alerta

        vehicle.IgnitionOn = ignitionOn;
        await db.SaveChangesAsync(cancellationToken);

        var lastPosition = await db.Positions
            .Where(p => p.VehicleId == vehicleId)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var referencePosition = new Position
        {
            VehicleId = vehicleId,
            Latitude = lastPosition?.Latitude ?? 0,
            Longitude = lastPosition?.Longitude ?? 0,
            Timestamp = timestampUtc
        };

        var companyGroup = PositionHub.CompanyGroup(vehicle.CompanyId);
        var type = ignitionOn ? AlertType.IgnitionOn : AlertType.IgnitionOff;
        var message = ignitionOn ? "Ignição ligada" : "Ignição desligada";

        await RaiseAlertAsync(db, vehicle, type, message, referencePosition, companyGroup, cancellationToken);
    }

    private async Task EvaluateEventsAsync(
        AppDbContext db,
        Vehicle vehicle,
        Position? previousPosition,
        Position position,
        string companyGroup,
        CancellationToken cancellationToken)
    {
        await CheckSpeedLimitAsync(db, vehicle, position, companyGroup, cancellationToken);
        await CheckGeofencesAsync(db, vehicle, previousPosition, position, companyGroup, cancellationToken);
    }

    private async Task CheckSpeedLimitAsync(
        AppDbContext db, Vehicle vehicle, Position position, string companyGroup, CancellationToken cancellationToken)
    {
        var limit = vehicle.SpeedLimitKmh ?? _defaultSpeedLimitKmh;
        if (position.Speed <= limit) return;

        await RaiseAlertAsync(db, vehicle, AlertType.SpeedLimitExceeded,
            $"Velocidade de {position.Speed:F1} km/h excede o limite de {limit:F1} km/h",
            position, companyGroup, cancellationToken);
    }

    private async Task CheckGeofencesAsync(
        AppDbContext db,
        Vehicle vehicle,
        Position? previousPosition,
        Position position,
        string companyGroup,
        CancellationToken cancellationToken)
    {
        var geofences = await db.Geofences
            .Where(g => g.CompanyId == vehicle.CompanyId && (g.VehicleId == null || g.VehicleId == vehicle.Id))
            .ToListAsync(cancellationToken);

        if (geofences.Count == 0) return;

        var currentPoint = GeometryFactory.CreatePoint(new Coordinate(position.Longitude, position.Latitude));
        var previousPoint = previousPosition is null
            ? null
            : GeometryFactory.CreatePoint(new Coordinate(previousPosition.Longitude, previousPosition.Latitude));

        foreach (var geofence in geofences)
        {
            bool isInside = geofence.Area.Contains(currentPoint);
            bool wasInside = previousPoint is not null && geofence.Area.Contains(previousPoint);

            if (isInside && !wasInside && geofence.AlertOnEnter)
            {
                await RaiseAlertAsync(db, vehicle, AlertType.GeofenceEnter,
                    $"Veículo entrou na área \"{geofence.Name}\"", position, companyGroup, cancellationToken);
            }
            else if (!isInside && wasInside && geofence.AlertOnExit)
            {
                await RaiseAlertAsync(db, vehicle, AlertType.GeofenceExit,
                    $"Veículo saiu da área \"{geofence.Name}\"", position, companyGroup, cancellationToken);
            }
        }
    }

    private async Task RaiseAlertAsync(
        AppDbContext db,
        Vehicle vehicle,
        AlertType type,
        string message,
        Position position,
        string companyGroup,
        CancellationToken cancellationToken)
    {
        var alert = new Alert
        {
            CompanyId = vehicle.CompanyId,
            VehicleId = vehicle.Id,
            Type = type,
            Message = message,
            Latitude = position.Latitude,
            Longitude = position.Longitude,
            Timestamp = position.Timestamp
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alerta disparado — veículo {VehicleId}: {Message}", vehicle.Id, message);

        await _hubContext.Clients.Group(companyGroup).SendAsync("AlertTriggered", new
        {
            id = alert.Id,
            vehicleId = alert.VehicleId,
            type = alert.Type.ToString(),
            message = alert.Message,
            latitude = alert.Latitude,
            longitude = alert.Longitude,
            timestamp = alert.Timestamp
        }, cancellationToken);
    }
}
