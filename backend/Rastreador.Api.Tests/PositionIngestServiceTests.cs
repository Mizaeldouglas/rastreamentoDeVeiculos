using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetTopologySuite.Geometries;
using Rastreador.Api.Data;
using Rastreador.Api.Hubs;
using Rastreador.Api.Models;
using Rastreador.Api.Services;
using Xunit;

namespace Rastreador.Api.Tests;

public class PositionIngestServiceTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PositionIngestService CreateService()
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<PositionHub>>();
        hubContext.Setup(h => h.Clients).Returns(clients.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alerts:DefaultSpeedLimitKmh"] = "100",
                ["Vapid:Subject"] = "mailto:test@test.com",
                ["Vapid:PublicKey"] = "BBcyzK2iStyda3gAzKObu4mlBxGIQHb_QpH5z37LgPH1Rftbtbd9690UA_cY3EjLhj9zgYTHiKXxKdTZluraDNs",
                ["Vapid:PrivateKey"] = "N7ErtjnTY-JOEl8Hp5eigP-GaionSxZ3OFcuUub7YQg",
            })
            .Build();

        var pushService = new PushNotificationService(configuration, NullLogger<PushNotificationService>.Instance);

        return new PositionIngestService(
            hubContext.Object, pushService, NullLogger<PositionIngestService>.Instance, configuration);
    }

    private static async Task<Vehicle> SeedVehicleAsync(AppDbContext db, double? speedLimitKmh = null)
    {
        var company = new Company { Name = "Empresa Teste" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            CompanyId = company.Id,
            Plate = "TST0001",
            Model = "Carro",
            Driver = "Motorista",
            SpeedLimitKmh = speedLimitKmh
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    private static Polygon BuildSquare(double minLat, double minLng, double maxLat, double maxLng)
    {
        var ring = GeometryFactory.CreateLinearRing(new[]
        {
            new Coordinate(minLng, minLat),
            new Coordinate(maxLng, minLat),
            new Coordinate(maxLng, maxLat),
            new Coordinate(minLng, maxLat),
            new Coordinate(minLng, minLat),
        });
        return GeometryFactory.CreatePolygon(ring);
    }

    [Fact]
    public async Task IngestAsync_SpeedAboveLimit_RaisesSpeedLimitExceededAlert()
    {
        using var db = CreateDb();
        var vehicle = await SeedVehicleAsync(db, speedLimitKmh: 60);
        var service = CreateService();

        await service.IngestAsync(db, vehicle.Id, -23.5, -46.6, speed: 80, heading: 0, DateTime.UtcNow, CancellationToken.None);

        var alerts = await db.Alerts.Where(a => a.VehicleId == vehicle.Id).ToListAsync();
        Assert.Single(alerts);
        Assert.Equal(AlertType.SpeedLimitExceeded, alerts[0].Type);
    }

    [Fact]
    public async Task IngestAsync_SpeedBelowLimit_DoesNotRaiseAlert()
    {
        using var db = CreateDb();
        var vehicle = await SeedVehicleAsync(db, speedLimitKmh: 100);
        var service = CreateService();

        await service.IngestAsync(db, vehicle.Id, -23.5, -46.6, speed: 50, heading: 0, DateTime.UtcNow, CancellationToken.None);

        var alerts = await db.Alerts.Where(a => a.VehicleId == vehicle.Id).ToListAsync();
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task IngestAsync_EnteringThenExitingGeofence_RaisesEnterAndExitAlerts()
    {
        using var db = CreateDb();
        var vehicle = await SeedVehicleAsync(db, speedLimitKmh: 200); // alto, para não interferir
        var service = CreateService();

        db.Geofences.Add(new Geofence
        {
            CompanyId = vehicle.CompanyId,
            Name = "Zona Teste",
            VehicleId = null,
            Area = BuildSquare(-23.6, -46.7, -23.5, -46.6),
            AlertOnEnter = true,
            AlertOnExit = true
        });
        await db.SaveChangesAsync();

        // Fora da geofence
        await service.IngestAsync(db, vehicle.Id, -23.0, -46.0, speed: 10, heading: 0, DateTime.UtcNow, CancellationToken.None);
        // Entra na geofence
        await service.IngestAsync(db, vehicle.Id, -23.55, -46.65, speed: 10, heading: 0, DateTime.UtcNow, CancellationToken.None);
        // Sai da geofence
        await service.IngestAsync(db, vehicle.Id, -23.0, -46.0, speed: 10, heading: 0, DateTime.UtcNow, CancellationToken.None);

        var alerts = await db.Alerts.Where(a => a.VehicleId == vehicle.Id).OrderBy(a => a.Id).ToListAsync();
        Assert.Equal(2, alerts.Count);
        Assert.Equal(AlertType.GeofenceEnter, alerts[0].Type);
        Assert.Equal(AlertType.GeofenceExit, alerts[1].Type);
    }

    [Fact]
    public async Task IngestIgnitionAsync_RepeatedSameState_DoesNotDuplicateAlert()
    {
        using var db = CreateDb();
        var vehicle = await SeedVehicleAsync(db);
        var service = CreateService();

        await service.IngestIgnitionAsync(db, vehicle.Id, ignitionOn: true, DateTime.UtcNow, CancellationToken.None);
        await service.IngestIgnitionAsync(db, vehicle.Id, ignitionOn: true, DateTime.UtcNow, CancellationToken.None);
        await service.IngestIgnitionAsync(db, vehicle.Id, ignitionOn: true, DateTime.UtcNow, CancellationToken.None);

        var alerts = await db.Alerts.Where(a => a.VehicleId == vehicle.Id).ToListAsync();
        Assert.Single(alerts);
        Assert.Equal(AlertType.IgnitionOn, alerts[0].Type);
    }

    [Fact]
    public async Task IngestIgnitionAsync_StateTransition_RaisesOneAlertPerTransition()
    {
        using var db = CreateDb();
        var vehicle = await SeedVehicleAsync(db);
        var service = CreateService();

        await service.IngestIgnitionAsync(db, vehicle.Id, ignitionOn: true, DateTime.UtcNow, CancellationToken.None);
        await service.IngestIgnitionAsync(db, vehicle.Id, ignitionOn: false, DateTime.UtcNow, CancellationToken.None);

        var alerts = await db.Alerts.Where(a => a.VehicleId == vehicle.Id).OrderBy(a => a.Id).ToListAsync();
        Assert.Equal(2, alerts.Count);
        Assert.Equal(AlertType.IgnitionOn, alerts[0].Type);
        Assert.Equal(AlertType.IgnitionOff, alerts[1].Type);

        var updatedVehicle = await db.Vehicles.FindAsync(vehicle.Id);
        Assert.False(updatedVehicle!.IgnitionOn);
    }
}
