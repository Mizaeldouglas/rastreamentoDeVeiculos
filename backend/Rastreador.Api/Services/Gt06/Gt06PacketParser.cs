using System.Buffers.Binary;

namespace Rastreador.Api.Services.Gt06;

/// <summary>
/// Parser para o protocolo GT06 (e clones como TK103), usado por rastreadores GPS baratos comuns no Brasil.
/// Suporta apenas os pacotes de login (0x01) e localização GPS (0x12), suficientes para um MVP de ingestão real.
/// </summary>
public static class Gt06PacketParser
{
    public const byte LoginProtocol = 0x01;
    public const byte LocationProtocol = 0x12;

    private static readonly byte[] StartBits = [0x78, 0x78];
    private static readonly byte[] StopBits = [0x0D, 0x0A];

    /// <summary>
    /// Tenta extrair um pacote completo do início do buffer. Retorna o número de bytes consumidos
    /// (0 se não houver pacote completo ainda) e os dados decodificados, se reconhecidos.
    /// </summary>
    public static bool TryParsePacket(
        ReadOnlySpan<byte> buffer,
        out int consumed,
        out byte protocolNumber,
        out ReadOnlySpan<byte> content,
        out ushort serial)
    {
        consumed = 0;
        protocolNumber = 0;
        content = default;
        serial = 0;

        if (buffer.Length < 5) return false;
        if (buffer[0] != StartBits[0] || buffer[1] != StartBits[1]) return false;

        byte length = buffer[2]; // protocolNumber(1) + content(N) + serial(2)
        int totalPacketLength = 2 + 1 + length + 2 + 2; // start(2) + lengthByte(1) + length + crc(2) + stop(2)

        if (buffer.Length < totalPacketLength) return false;
        if (length < 3) return false; // precisa ao menos protocolNumber + serial

        var stopSpan = buffer.Slice(totalPacketLength - 2, 2);
        if (stopSpan[0] != StopBits[0] || stopSpan[1] != StopBits[1]) return false;

        protocolNumber = buffer[3];
        int contentLength = length - 1 - 2; // remove protocolNumber e serial
        content = buffer.Slice(4, contentLength);
        serial = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4 + contentLength, 2));

        var crcSpan = buffer.Slice(4 + contentLength + 2, 2);
        var expectedCrc = BinaryPrimitives.ReadUInt16BigEndian(crcSpan);
        var crcInput = buffer.Slice(2, 1 + length); // lengthByte + protocolNumber + content + serial
        var actualCrc = Gt06Crc.Compute(crcInput);

        consumed = totalPacketLength;
        return actualCrc == expectedCrc;
    }

    public static Gt06LoginPacket ParseLogin(ReadOnlySpan<byte> content, ushort serial)
    {
        // IMEI vem como BCD: cada byte = 2 dígitos decimais
        var sb = new System.Text.StringBuilder(content.Length * 2);
        foreach (var b in content)
        {
            sb.Append((b >> 4) & 0x0F);
            sb.Append(b & 0x0F);
        }

        var imei = sb.ToString().TrimStart('0');
        return new Gt06LoginPacket(imei, serial);
    }

    public static Gt06LocationPacket ParseLocation(ReadOnlySpan<byte> content, ushort serial)
    {
        // content: YY MM DD HH MM SS (6) + gpsInfo(1) + lat(4) + lng(4) + speed(1) + courseStatus(2)
        int year = 2000 + content[0];
        int month = content[1];
        int day = content[2];
        int hour = content[3];
        int minute = content[4];
        int second = content[5];
        var timestamp = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

        uint rawLat = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(7, 4));
        uint rawLng = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(11, 4));
        double latitude = rawLat / 30000.0 / 60.0;
        double longitude = rawLng / 30000.0 / 60.0;

        double speed = content[15];
        ushort courseStatus = BinaryPrimitives.ReadUInt16BigEndian(content.Slice(16, 2));

        const ushort southFlag = 0x0800;
        const ushort westFlag = 0x1000;
        double course = courseStatus & 0x03FF;

        if ((courseStatus & southFlag) != 0) latitude = -latitude;
        if ((courseStatus & westFlag) != 0) longitude = -longitude;

        return new Gt06LocationPacket(timestamp, latitude, longitude, speed, course, serial);
    }

    /// <summary>Monta o pacote de ACK enviado ao dispositivo após um login bem-sucedido.</summary>
    public static byte[] BuildLoginAck(ushort serial)
    {
        // length = protocolNumber(1) + content(0) + serial(2), sem incluir o próprio byte de length nem o crc
        const byte length = 3;
        var packet = new byte[2 + 1 + length + 2 + 2]; // start + lengthByte + (protocol+serial) + crc + stop = 10 bytes
        packet[0] = StartBits[0];
        packet[1] = StartBits[1];
        packet[2] = length;
        packet[3] = LoginProtocol;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), serial);

        var crc = Gt06Crc.Compute(packet.AsSpan(2, 1 + length));
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(6, 2), crc);

        packet[8] = StopBits[0];
        packet[9] = StopBits[1];
        return packet;
    }
}
