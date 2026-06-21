import { useState } from "react";
import { useFleetData } from "../context/FleetDataContext";
import { HistoryPlayback } from "../components/HistoryPlayback";
import { MapView } from "../components/MapView";
import type { LatLngDto } from "../types/geofence";

export function HistoryPage() {
  const { vehicles } = useFleetData();
  const [historyRoute, setHistoryRoute] = useState<LatLngDto[]>([]);
  const [historyMarker, setHistoryMarker] = useState<LatLngDto | null>(null);

  return (
    <div className="page">
      <div>
        <h1 className="page-title">Histórico de rotas</h1>
        <p className="page-subtitle">Busque um período e reproduza o trajeto percorrido</p>
      </div>

      <section className="card">
        <HistoryPlayback
          vehicles={vehicles}
          onChange={(route, marker) => {
            setHistoryRoute(route);
            setHistoryMarker(marker);
          }}
        />
      </section>

      <section className="card map-card">
        <MapView
          vehicles={[]}
          historyRoute={historyRoute}
          historyMarker={historyMarker}
          height="500px"
        />
      </section>
    </div>
  );
}
