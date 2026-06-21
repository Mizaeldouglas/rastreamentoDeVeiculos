using System.Buffers.Binary;
using System.Net.Sockets;

// Simulador de um rastreador GPS real falando o protocolo GT06 via TCP.
// Uso: dotnet run -- <imei> [host] [port]
// Exemplo: dotnet run -- 123456789012345 localhost 5023

string imei = args.Length > 0 ? args[0] : "123456789012345";
string host = args.Length > 1 ? args[1] : "localhost";
int port = args.Length > 2 ? int.Parse(args[2]) : 5023;

Console.WriteLine($"Conectando a {host}:{port} simulando dispositivo IMEI {imei}...");

using var client = new TcpClient();
await client.ConnectAsync(host, port);
var stream = client.GetStream();

Console.WriteLine("Conectado. Enviando login...");

ushort serial = 1;
var loginPacket = BuildLoginPacket(imei, serial++);
await stream.WriteAsync(loginPacket);

var ackBuffer = new byte[16];
int ackRead = await stream.ReadAsync(ackBuffer);
Console.WriteLine($"ACK recebido ({ackRead} bytes): {Convert.ToHexString(ackBuffer.AsSpan(0, ackRead))}");

var random = new Random();
double latitude = -22.570796574008 + (random.NextDouble() - 0.2) * 0.1;
double longitude = -47.40573495216076 + (random.NextDouble() - 0.2) * 0.1;
double heading = random.NextDouble() * 360;

Console.WriteLine("Enviando posições simuladas a cada 3s (Ctrl+C para parar)...");

while (true)
{
    heading = (heading + (random.NextDouble() - 0.5) * 30 + 360) % 360;
    double stepFraction = random.NextDouble();
    const double maxStepDegrees = 0.0015;
    double step = maxStepDegrees * stepFraction;
    double headingRad = heading * Math.PI / 180;

    latitude += step * Math.Cos(headingRad);
    longitude += step * Math.Sin(headingRad);
    double speed = Math.Round(stepFraction * 90, 1);

    var packet = BuildLocationPacket(DateTime.UtcNow, latitude, longitude, speed, heading, serial++);
    await stream.WriteAsync(packet);

    Console.WriteLine($"Posição enviada: lat={latitude:F6} lng={longitude:F6} speed={speed} heading={heading:F1}");

    await Task.Delay(TimeSpan.FromSeconds(3));
}

static byte[] BuildLoginPacket(string imei, ushort serial)
{
    var imeiDigits = imei.PadLeft(16, '0');
    var content = new byte[8];
    for (int i = 0; i < 8; i++)
    {
        int high = imeiDigits[i * 2] - '0';
        int low = imeiDigits[i * 2 + 1] - '0';
        content[i] = (byte)((high << 4) | low);
    }

    return BuildPacket(protocolNumber: 0x01, content, serial);
}

static byte[] BuildLocationPacket(DateTime utc, double latitude, double longitude, double speedKmh, double heading, ushort serial)
{
    var content = new byte[18];
    content[0] = (byte)(utc.Year - 2000);
    content[1] = (byte)utc.Month;
    content[2] = (byte)utc.Day;
    content[3] = (byte)utc.Hour;
    content[4] = (byte)utc.Minute;
    content[5] = (byte)utc.Second;
    content[6] = 0x0C; // gps info length (4 bits) + satelites (4 bits) — valor fixo plausível

    bool south = latitude < 0;
    bool west = longitude < 0;
    uint rawLat = (uint)Math.Round(Math.Abs(latitude) * 30000.0 * 60.0);
    uint rawLng = (uint)Math.Round(Math.Abs(longitude) * 30000.0 * 60.0);

    BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(7, 4), rawLat);
    BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(11, 4), rawLng);

    content[15] = (byte)Math.Min(255, Math.Round(speedKmh));

    ushort courseStatus = (ushort)(((ushort)Math.Round(heading)) & 0x03FF);
    if (south) courseStatus |= 0x0800;
    if (west) courseStatus |= 0x1000;
    BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(16, 2), courseStatus);

    return BuildPacket(protocolNumber: 0x12, content, serial);
}

static byte[] BuildPacket(byte protocolNumber, byte[] content, ushort serial)
{
    int length = 1 + content.Length + 2; // protocolNumber + content + serial
    var packet = new byte[2 + 1 + length + 2 + 2];

    packet[0] = 0x78;
    packet[1] = 0x78;
    packet[2] = (byte)length;
    packet[3] = protocolNumber;
    content.CopyTo(packet, 4);
    BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4 + content.Length, 2), serial);

    var crc = Gt06Crc.Compute(packet.AsSpan(2, 1 + length));
    BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4 + content.Length + 2, 2), crc);

    packet[^2] = 0x0D;
    packet[^1] = 0x0A;
    return packet;
}

static class Gt06Crc
{
    private static readonly ushort[] Table = BuildTable();

    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc = (ushort)((crc >> 8) ^ Table[(crc ^ b) & 0xFF]);
        }
        return (ushort)~crc;
    }

    private static ushort[] BuildTable()
    {
        const ushort polynomial = 0x8408;
        var table = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            ushort value = (ushort)i;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0
                    ? (ushort)((value >> 1) ^ polynomial)
                    : (ushort)(value >> 1);
            }
            table[i] = value;
        }
        return table;
    }
}
