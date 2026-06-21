import { useCallback, useEffect, useState } from "react";
import { geofencesApi, vehiclesApi } from "./services/api";
import { getSession, logout, onUnauthorized } from "./services/auth";
import { VehicleForm } from "./components/VehicleForm";
import { VehicleList } from "./components/VehicleList";
import { MapView } from "./components/MapView";
import { AlertsPanel } from "./components/AlertsPanel";
import { GeofenceManager } from "./components/GeofenceManager";
import { HistoryPlayback } from "./components/HistoryPlayback";
import { LoginPage } from "./components/LoginPage";
import type { VehicleCreateDto, VehicleDto } from "./types/vehicle";
import type { GeofenceDto, LatLngDto } from "./types/geofence";
import type { AuthResponseDto } from "./types/auth";
import "./App.css";

function Dashboard({ session, onLogout }: { session: AuthResponseDto; onLogout: () => void }) {
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [geofences, setGeofences] = useState<GeofenceDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [historyRoute, setHistoryRoute] = useState<LatLngDto[]>([]);
  const [historyMarker, setHistoryMarker] = useState<LatLngDto | null>(null);

  const loadVehicles = useCallback(async () => {
    try {
      const data = await vehiclesApi.list();
      setVehicles(data);
      setError(null);
    } catch (err) {
      console.error(err);
      setError("Não foi possível conectar à API. Verifique se o backend está rodando.");
    }
  }, []);

  const loadGeofences = useCallback(async () => {
    try {
      const data = await geofencesApi.list();
      setGeofences(data);
    } catch (err) {
      console.error(err);
    }
  }, []);

  useEffect(() => {
    loadVehicles();
    loadGeofences();
    const interval = setInterval(() => {
      loadVehicles();
      loadGeofences();
    }, 5000);
    return () => clearInterval(interval);
  }, [loadVehicles, loadGeofences]);

  const handleCreate = async (dto: VehicleCreateDto) => {
    await vehiclesApi.create(dto);
    await loadVehicles();
  };

  const handleDelete = async (id: number) => {
    await vehiclesApi.remove(id);
    await loadVehicles();
  };

  return (
    <div className="app-shell">
      <header className="topbar">
        <div>
          <h1>Rastreador Veicular</h1>
          <p className="subtitle">{session.companyName} — Monitoramento em tempo real</p>
        </div>
        <button className="btn btn-sm" onClick={onLogout}>
          Sair
        </button>
      </header>

      {error && <div className="alert alert-error">{error}</div>}

      <section className="card">
        <h2 className="section-title">Novo veículo</h2>
        <VehicleForm onSubmit={handleCreate} />
      </section>

      <section className="card">
        <h2 className="section-title">Veículos cadastrados</h2>
        <VehicleList vehicles={vehicles} onDelete={handleDelete} />
      </section>

      <section className="card map-card">
        <h2 className="section-title">Mapa em tempo real</h2>
        <MapView
          vehicles={vehicles}
          geofences={geofences}
          historyRoute={historyRoute}
          historyMarker={historyMarker}
        />
      </section>

      <section className="card">
        <h2 className="section-title">Geofences</h2>
        <GeofenceManager vehicles={vehicles} geofences={geofences} onChange={loadGeofences} />
      </section>

      <section className="card">
        <h2 className="section-title">Histórico de rotas</h2>
        <HistoryPlayback
          vehicles={vehicles}
          onChange={(route, marker) => {
            setHistoryRoute(route);
            setHistoryMarker(marker);
          }}
        />
      </section>

      <section className="card">
        <h2 className="section-title">Alertas recentes</h2>
        <AlertsPanel />
      </section>
    </div>
  );
}

function App() {
  const [session, setSession] = useState<AuthResponseDto | null>(() => getSession());

  useEffect(() => {
    onUnauthorized(() => setSession(null));
  }, []);

  const handleLogout = () => {
    logout();
    setSession(null);
  };

  if (!session) {
    return <LoginPage onAuthenticated={setSession} />;
  }

  return <Dashboard session={session} onLogout={handleLogout} />;
}

export default App;
