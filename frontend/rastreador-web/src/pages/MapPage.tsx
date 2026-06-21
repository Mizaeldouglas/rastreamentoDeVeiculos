import { useFleetData } from "../context/FleetDataContext";
import { MapView } from "../components/MapView";

export function MapPage() {
  const { vehicles, geofences } = useFleetData();

  return (
    <div className="page">
      <div>
        <h1 className="page-title">Mapa em tempo real</h1>
        <p className="page-subtitle">Posições atuais da frota e geofences cadastradas</p>
      </div>

      <section className="card map-card">
        <MapView vehicles={vehicles} geofences={geofences} height="calc(100vh - 220px)" />
      </section>
    </div>
  );
}
