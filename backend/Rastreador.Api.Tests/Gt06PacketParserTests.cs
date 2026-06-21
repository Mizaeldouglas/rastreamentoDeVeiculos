using System.Buffers.Binary;
using Rastreador.Api.Services.Gt06;
using Xunit;

namespace Rastreador.Api.Tests;

public class Gt06PacketParserTests
{
    private static byte[] BuildPacket(byte protocolNumber, byte[] content, ushort serial)
    {
        int length = 1 + content.Length + 2;
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

    private static byte[] BuildLoginContent(string imei)
    {
        var imeiDigits = imei.PadLeft(16, '0');
        var content = new byte[8];
        for (int i = 0; i < 8; i++)
        {
            int high = imeiDigits[i * 2] - '0';
            int low = imeiDigits[i * 2 + 1] - '0';
            content[i] = (byte)((high << 4) | low);
        }
        return content;
    }

    [Fact]
    public void TryParsePacket_ValidCrc_ReturnsTrueAndConsumesWholePacket()
    {
        var packet = BuildPacket(Gt06PacketParser.LoginProtocol, BuildLoginContent("123456789012345"), serial: 7);

        var result = Gt06PacketParser.TryParsePacket(packet, out var consumed, out var protocolNumber, out var content, out var serial);

        Assert.True(result);
        Assert.Equal(packet.Length, consumed);
        Assert.Equal(Gt06PacketParser.LoginProtocol, protocolNumber);
        Assert.Equal(7, serial);
        Assert.Equal(8, content.Length);
    }

    [Fact]
    public void TryParsePacket_CorruptedCrc_ReturnsFalse()
    {
        var packet = BuildPacket(Gt06PacketParser.LoginProtocol, BuildLoginContent("123456789012345"), serial: 7);
        packet[^3] ^= 0xFF; // corrompe um byte do CRC

        var result = Gt06PacketParser.TryParsePacket(packet, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParsePacket_IncompleteBuffer_ReturnsFalseWithZeroConsumed()
    {
        var packet = BuildPacket(Gt06PacketParser.LoginProtocol, BuildLoginContent("123456789012345"), serial: 7);
        var partial = packet[..^3];

        var result = Gt06PacketParser.TryParsePacket(partial, out var consumed, out _, out _, out _);

        Assert.False(result);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void ParseLogin_DecodesImeiFromBcd()
    {
        var content = BuildLoginContent("123456789012345");

        var login = Gt06PacketParser.ParseLogin(content, serial: 1);

        Assert.Equal("123456789012345", login.Imei);
    }

    [Fact]
    public void ParseLocation_DecodesCoordinatesSpeedAndHemisphereFlags()
    {
        var content = new byte[18];
        content[0] = 26; // ano 2026
        content[1] = 6;
        content[2] = 21;
        content[3] = 12;
        content[4] = 30;
        content[5] = 0;
        content[6] = 0x0C;

        // -22.5 graus e -47.4 graus, codificados como valor/30000/60 e marcados sul/oeste
        uint rawLat = (uint)Math.Round(22.5 * 30000.0 * 60.0);
        uint rawLng = (uint)Math.Round(47.4 * 30000.0 * 60.0);
        BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(7, 4), rawLat);
        BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(11, 4), rawLng);

        content[15] = 80; // velocidade

        ushort courseStatus = 180; // curso
        courseStatus |= 0x0800; // sul
        courseStatus |= 0x1000; // oeste
        BinaryPrimitives.WriteUInt16BigEndian(content.AsSpan(16, 2), courseStatus);

        var location = Gt06PacketParser.ParseLocation(content, serial: 9);

        Assert.Equal(-22.5, location.Latitude, precision: 3);
        Assert.Equal(-47.4, location.Longitude, precision: 3);
        Assert.Equal(80, location.SpeedKmh);
        Assert.Equal(180, location.Course);
        Assert.Equal(new DateTime(2026, 6, 21, 12, 30, 0, DateTimeKind.Utc), location.TimestampUtc);
    }

    [Theory]
    [InlineData(0x02, true)]
    [InlineData(0x00, false)]
    public void ParseStatus_ReadsIgnitionBit(byte terminalInfo, bool expectedIgnitionOn)
    {
        var status = Gt06PacketParser.ParseStatus([terminalInfo], serial: 3);

        Assert.Equal(expectedIgnitionOn, status.IgnitionOn);
    }

    [Fact]
    public void BuildLoginAck_ProducesPacketThatParsesBackWithMatchingSerial()
    {
        var ack = Gt06PacketParser.BuildLoginAck(serial: 42);

        var result = Gt06PacketParser.TryParsePacket(ack, out _, out var protocolNumber, out _, out var serial);

        Assert.True(result);
        Assert.Equal(Gt06PacketParser.LoginProtocol, protocolNumber);
        Assert.Equal(42, serial);
    }
}
