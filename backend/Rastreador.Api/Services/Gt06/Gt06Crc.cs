namespace Rastreador.Api.Services.Gt06;

public static class Gt06Crc
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
        const ushort polynomial = 0x8408; // CRC16/X25 (reversed 0x1021)
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
