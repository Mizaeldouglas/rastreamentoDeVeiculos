using NetTopologySuite.Geometries;

namespace Rastreador.Api.Models;

public class Geofence
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Null = aplica a todos os veículos.</summary>
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public Polygon Area { get; set; } = null!;
    public bool AlertOnEnter { get; set; } = true;
    public bool AlertOnExit { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
