namespace Rastreador.Api.Models;

public class AlertDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Acknowledged { get; set; }
}
