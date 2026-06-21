namespace Rastreador.Api.Models;

public class VehicleDto
{
    public int Id { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public double? SpeedLimitKmh { get; set; }
    public DateTime CreatedAt { get; set; }
    public PositionDto? LastPosition { get; set; }
}

public class PositionDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double Heading { get; set; }
    public DateTime Timestamp { get; set; }
}

public class VehicleCreateDto
{
    public string Plate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public double? SpeedLimitKmh { get; set; }
}
