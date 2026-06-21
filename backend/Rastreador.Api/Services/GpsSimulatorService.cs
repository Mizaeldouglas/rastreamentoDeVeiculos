using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Models;

namespace Rastreador.Api.Services;

public class GpsSimulatorService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);
    private const double MaxStepDegrees = 0.0015; // ~ poucas centenas de metros por tick
    private const double MaxSpeedKmh = 90;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GpsSimulatorService> _logger;
    private readonly Random _random = new();

    public GpsSimulatorService(
        IServiceScopeFactory scopeFactory,
        ILogger<GpsSimulatorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar posições simuladas");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<PositionIngestService>();

        // Veículos com Imei recebem posições reais via GpsTcpListenerService — não simular para eles.
        var vehicles = await db.Vehicles.Where(v => v.Imei == null).ToListAsync(cancellationToken);
        if (vehicles.Count == 0) return;

        foreach (var vehicle in vehicles)
        {
            var last = await db.Positions
                .Where(p => p.VehicleId == vehicle.Id)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            var (latitude, longitude, speed, heading) = GenerateNextPosition(last);

            await ingestService.IngestAsync(
                db, vehicle.Id, latitude, longitude, speed, heading, DateTime.UtcNow, cancellationToken);
        }
    }

    private (double Latitude, double Longitude, double Speed, double Heading) GenerateNextPosition(Position? last)
    {
        // Origem padrão: São Paulo, caso o veículo ainda não tenha histórico
        var baseLat = last?.Latitude ?? -23.5505 + (_random.NextDouble() - 0.5) * 0.1;
        var baseLng = last?.Longitude ?? -46.6333 + (_random.NextDouble() - 0.5) * 0.1;

        var heading = last?.Heading ?? _random.NextDouble() * 360;
        // pequena variação de direção a cada tick, simulando curvas suaves
        heading = (heading + (_random.NextDouble() - 0.5) * 30 + 360) % 360;

        var stepFraction = _random.NextDouble();
        var step = MaxStepDegrees * stepFraction;
        var headingRad = heading * Math.PI / 180;

        var newLat = baseLat + step * Math.Cos(headingRad);
        var newLng = baseLng + step * Math.Sin(headingRad);
        var speed = Math.Round(stepFraction * MaxSpeedKmh, 1);

        return (newLat, newLng, speed, Math.Round(heading, 1));
    }
}
