using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using Rastreador.Api.Services.Gt06;

namespace Rastreador.Api.Services;

/// <summary>
/// Aceita conexões TCP de rastreadores GPS reais (protocolo GT06) e processa login + posições.
/// </summary>
public class GpsTcpListenerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GpsTcpListenerService> _logger;
    private readonly int _port;

    public GpsTcpListenerService(
        IServiceScopeFactory scopeFactory,
        ILogger<GpsTcpListenerService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _port = configuration.GetValue("Gps:TcpPort", 5023);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("GpsTcpListenerService aguardando dispositivos GT06 na porta {Port}", _port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        var endpoint = client.Client.RemoteEndPoint;
        _logger.LogInformation("Dispositivo conectado: {Endpoint}", endpoint);
        string? imei = null;

        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            var pending = new List<byte>();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    int read = await stream.ReadAsync(buffer, stoppingToken);
                    if (read == 0) break;

                    pending.AddRange(buffer.AsSpan(0, read).ToArray());

                    while (TryDequeuePacket(pending, out var packet))
                    {
                        await ProcessPacketAsync(packet.ProtocolNumber, packet.Content, packet.Serial, stream, stoppingToken,
                            value => imei = value, () => imei);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                // conexão encerrada pelo dispositivo ou pelo shutdown do serviço
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar dados do dispositivo {Endpoint}", endpoint);
            }
        }

        _logger.LogInformation("Dispositivo desconectado: {Endpoint} (IMEI {Imei})", endpoint, imei);
    }

    private readonly record struct ParsedPacket(byte ProtocolNumber, byte[] Content, ushort Serial);

    private static bool TryDequeuePacket(List<byte> pending, out ParsedPacket packet)
    {
        var array = pending.ToArray();
        if (Gt06PacketParser.TryParsePacket(array, out var consumed, out var protocolNumber, out var content, out var serial)
            && consumed > 0)
        {
            packet = new ParsedPacket(protocolNumber, content.ToArray(), serial);
            pending.RemoveRange(0, consumed);
            return true;
        }

        packet = default;
        return false;
    }

    private async Task ProcessPacketAsync(
        byte protocolNumber,
        byte[] content,
        ushort serial,
        NetworkStream stream,
        CancellationToken stoppingToken,
        Action<string> setImei,
        Func<string?> getImei)
    {
        if (protocolNumber == Gt06PacketParser.LoginProtocol)
        {
            var login = Gt06PacketParser.ParseLogin(content, serial);
            setImei(login.Imei);
            _logger.LogInformation("Login recebido — IMEI {Imei}", login.Imei);

            var ack = Gt06PacketParser.BuildLoginAck(serial);
            await stream.WriteAsync(ack, stoppingToken);
            return;
        }

        if (protocolNumber == Gt06PacketParser.LocationProtocol)
        {
            var imei = getImei();
            if (imei is null)
            {
                _logger.LogWarning("Pacote de localização recebido antes do login — ignorando");
                return;
            }

            var location = Gt06PacketParser.ParseLocation(content, serial);
            await PersistAndBroadcastAsync(imei, location, stoppingToken);
            return;
        }

        if (protocolNumber == Gt06PacketParser.StatusProtocol)
        {
            var imei = getImei();
            if (imei is null)
            {
                _logger.LogWarning("Pacote de status recebido antes do login — ignorando");
                return;
            }

            var status = Gt06PacketParser.ParseStatus(content, serial);
            await PersistIgnitionAsync(imei, status, stoppingToken);
        }
    }

    private async Task PersistAndBroadcastAsync(string imei, Gt06LocationPacket location, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<PositionIngestService>();

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Imei == imei, stoppingToken);
        if (vehicle is null)
        {
            _logger.LogWarning("Nenhum veículo cadastrado com o IMEI {Imei} — posição descartada", imei);
            return;
        }

        await ingestService.IngestAsync(
            db, vehicle.Id, location.Latitude, location.Longitude, location.SpeedKmh, location.Course,
            location.TimestampUtc, stoppingToken);
    }

    private async Task PersistIgnitionAsync(string imei, Gt06StatusPacket status, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<PositionIngestService>();

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Imei == imei, stoppingToken);
        if (vehicle is null)
        {
            _logger.LogWarning("Nenhum veículo cadastrado com o IMEI {Imei} — status descartado", imei);
            return;
        }

        await ingestService.IngestIgnitionAsync(db, vehicle.Id, status.IgnitionOn, DateTime.UtcNow, stoppingToken);
    }
}
