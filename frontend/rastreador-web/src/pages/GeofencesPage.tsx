import { useFleetData } from "../context/FleetDataContext";
import { GeofenceManager } from "../components/GeofenceManager";

export function GeofencesPage() {
  const { vehicles, geofences, reloadGeofences } = useFleetData();

  return (
    <div className="page">
      <div>
        <h1 className="page-title">Geofences</h1>
        <p className="page-subtitle">Áreas monitoradas para alertas de entrada/saída</p>
      </div>

      <section className="card">
        <GeofenceManager vehicles={vehicles} geofences={geofences} onChange={reloadGeofences} />
      </section>
    </div>
  );
}
