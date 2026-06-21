import { AlertsPanel } from "../components/AlertsPanel";

export function AlertsPage() {
  return (
    <div className="page">
      <div>
        <h1 className="page-title">Alertas</h1>
        <p className="page-subtitle">Eventos de geofence, velocidade e ignição em tempo real</p>
      </div>

      <section className="card">
        <AlertsPanel />
      </section>
    </div>
  );
}
