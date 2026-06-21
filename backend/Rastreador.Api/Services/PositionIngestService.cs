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

        await _hubContext.Clients.All.SendAsync("PositionUpdated", new
        {
            vehicleId = position.VehicleId,
            latitude = position.Latitude,
            longitude = position.Longitude,
            speed = position.Speed,
            heading = position.Heading,
            timestamp = position.Timestamp
        }, cancellationToken);

        await EvaluateEventsAsync(db, vehicleId, previousPosition, position, cancellationToken);
    }

    private async Task EvaluateEventsAsync(
        AppDbContext db,
        int vehicleId,
        Position? previousPosition,
        Position position,
        CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FindAsync([vehicleId], cancellationToken);
        if (vehicle is null) return;

        await CheckSpeedLimitAsync(db, vehicle, position, cancellationToken);
        await CheckGeofencesAsync(db, vehicle, previousPosition, position, cancellationToken);
    }

    private async Task CheckSpeedLimitAsync(AppDbContext db, Vehicle vehicle, Position position, CancellationToken cancellationToken)
    {
        var limit = vehicle.SpeedLimitKmh ?? _defaultSpeedLimitKmh;
        if (position.Speed <= limit) return;

        await RaiseAlertAsync(db, vehicle.Id, AlertType.SpeedLimitExceeded,
            $"Velocidade de {position.Speed:F1} km/h excede o limite de {limit:F1} km/h",
            position, cancellationToken);
    }

    private async Task CheckGeofencesAsync(
        AppDbContext db,
        Vehicle vehicle,
        Position? previousPosition,
        Position position,
        CancellationToken cancellationToken)
    {
        var geofences = await db.Geofences
            .Where(g => g.VehicleId == null || g.VehicleId == vehicle.Id)
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
                await RaiseAlertAsync(db, vehicle.Id, AlertType.GeofenceEnter,
                    $"Veículo entrou na área \"{geofence.Name}\"", position, cancellationToken);
            }
            else if (!isInside && wasInside && geofence.AlertOnExit)
            {
                await RaiseAlertAsync(db, vehicle.Id, AlertType.GeofenceExit,
                    $"Veículo saiu da área \"{geofence.Name}\"", position, cancellationToken);
            }
        }
    }

    private async Task RaiseAlertAsync(
        AppDbContext db,
        int vehicleId,
        AlertType type,
        string message,
        Position position,
        CancellationToken cancellationToken)
    {
        var alert = new Alert
        {
            VehicleId = vehicleId,
            Type = type,
            Message = message,
            Latitude = position.Latitude,
            Longitude = position.Longitude,
            Timestamp = position.Timestamp
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alerta disparado — veículo {VehicleId}: {Message}", vehicleId, message);

        await _hubContext.Clients.All.SendAsync("AlertTriggered", new
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
