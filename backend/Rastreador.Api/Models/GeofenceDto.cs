namespace Rastreador.Api.Models;

public class LatLngDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class GeofenceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? VehicleId { get; set; }
    public List<LatLngDto> Points { get; set; } = [];
    public bool AlertOnEnter { get; set; }
    public bool AlertOnExit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeofenceCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int? VehicleId { get; set; }
    public List<LatLngDto> Points { get; set; } = [];
    public bool AlertOnEnter { get; set; } = true;
    public bool AlertOnExit { get; set; } = true;
}
