namespace Rastreador.Api.Models;

public enum AlertType
{
    GeofenceEnter,
    GeofenceExit,
    SpeedLimitExceeded
}

public class Alert
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public AlertType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Acknowledged { get; set; }
}
