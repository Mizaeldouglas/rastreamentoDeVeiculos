namespace Rastreador.Api.Services.Gt06;

public record Gt06LoginPacket(string Imei, ushort Serial);

public record Gt06LocationPacket(
    DateTime TimestampUtc,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Course,
    ushort Serial);
