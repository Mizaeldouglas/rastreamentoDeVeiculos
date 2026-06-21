import { useEffect, useState } from "react";
import { Truck, Gauge, Power, Wifi, AlertTriangle, BellRing } from "lucide-react";
import { useFleetData } from "../context/FleetDataContext";
import { MapView } from "../components/MapView";
import { alertsApi } from "../services/api";
import type { AlertDto } from "../types/alert";
import "./DashboardPage.css";

const ONLINE_THRESHOLD_MS = 5 * 60 * 1000;
const ALERTS_POLL_MS = 5000;

const ALERT_TYPE_LABELS: Record<AlertDto["type"], string> = {
  GeofenceEnter: "Entrou na área",
  GeofenceExit: "Saiu da área",
  SpeedLimitExceeded: "Excesso de velocidade",
  IgnitionOn: "Ignição ligada",
  IgnitionOff: "Ignição desligada",
};

export function DashboardPage() {
  const { vehicles } = useFleetData();
  const [alerts, setAlerts] = useState<AlertDto[]>([]);

  useEffect(() => {
    const load = () => alertsApi.list().then(setAlerts).catch(console.error);
    load();
    const interval = setInterval(load, ALERTS_POLL_MS);
    return () => clearInterval(interval);
  }, []);

  const totalVehicles = vehicles.length;
  const moving = vehicles.filter((v) => (v.lastPosition?.speed ?? 0) > 0).length;
  const ignitionOn = vehicles.filter((v) => v.ignitionOn === true).length;
  const online = vehicles.filter(
    (v) => v.lastPosition && Date.now() - new Date(v.lastPosition.timestamp).getTime() < ONLINE_THRESHOLD_MS
  ).length;

  const last24h = alerts.filter(
    (a) => Date.now() - new Date(a.timestamp).getTime() < 24 * 60 * 60 * 1000
  ).length;
  const unacknowledged = alerts.filter((a) => !a.acknowledged);

  return (
    <div className="page">
      <div>
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Visão geral da frota e alertas de segurança</p>
      </div>

      <div className="metrics-grid">
        <div className="metric-card">
          <div className="metric-icon metric-icon-primary">
            <Truck size={20} />
          </div>
          <div>
            <p className="metric-value">{totalVehicles}</p>
            <p className="metric-label">Veículos cadastrados</p>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon metric-icon-success">
            <Gauge size={20} />
          </div>
          <div>
            <p className="metric-value">{moving}</p>
            <p className="metric-label">Em movimento agora</p>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon metric-icon-warning">
            <Power size={20} />
          </div>
          <div>
            <p className="metric-value">{ignitionOn}</p>
            <p className="metric-label">Com ignição ligada</p>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon metric-icon-success">
            <Wifi size={20} />
          </div>
          <div>
            <p className="metric-value">{online}</p>
            <p className="metric-label">Online (últimos 5 min)</p>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon metric-icon-danger">
            <AlertTriangle size={20} />
          </div>
          <div>
            <p className="metric-value">{last24h}</p>
            <p className="metric-label">Alertas nas últimas 24h</p>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon metric-icon-danger">
            <BellRing size={20} />
          </div>
          <div>
            <p className="metric-value">{unacknowledged.length}</p>
            <p className="metric-label">Alertas não reconhecidos</p>
          </div>
        </div>
      </div>

      <div className="dashboard-columns">
        <section className="card map-card dashboard-map">
          <MapView vehicles={vehicles} height="420px" />
        </section>

        <section className="card dashboard-alerts">
          <h2 className="section-title">Alertas recentes não reconhecidos</h2>
          {unacknowledged.length === 0 ? (
            <p className="empty-state">Nenhum alerta pendente. Tudo certo por aqui.</p>
          ) : (
            <ul className="dashboard-alert-list">
              {unacknowledged.slice(0, 5).map((alert) => (
                <li key={alert.id} className="dashboard-alert-item">
                  <span className="dashboard-alert-type">{ALERT_TYPE_LABELS[alert.type]}</span>
                  <span className="dashboard-alert-message">{alert.message}</span>
                  <span className="dashboard-alert-time">
                    {new Date(alert.timestamp).toLocaleTimeString()}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}
