namespace Rastreador.Api.Models;

public class Vehicle
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public double? SpeedLimitKmh { get; set; }
    public bool? IgnitionOn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Geofence> Geofences { get; set; } = new List<Geofence>();
}
