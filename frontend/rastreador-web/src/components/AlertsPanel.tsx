import { useEffect, useRef, useState } from "react";
import { alertsApi } from "../services/api";
import { createPositionConnection } from "../services/signalr";
import type { AlertDto } from "../types/alert";
import "./AlertsPanel.css";

const TYPE_LABELS: Record<AlertDto["type"], string> = {
  GeofenceEnter: "Entrou na área",
  GeofenceExit: "Saiu da área",
  SpeedLimitExceeded: "Excesso de velocidade",
};

const TYPE_CLASS: Record<AlertDto["type"], string> = {
  GeofenceEnter: "badge-enter",
  GeofenceExit: "badge-exit",
  SpeedLimitExceeded: "badge-speed",
};

export function AlertsPanel() {
  const [alerts, setAlerts] = useState<AlertDto[]>([]);
  const connectionRef = useRef<ReturnType<typeof createPositionConnection> | null>(null);

  useEffect(() => {
    alertsApi.list().then(setAlerts).catch(console.error);

    const connection = createPositionConnection(undefined, (event) => {
      setAlerts((prev) => [event, ...prev].slice(0, 200));
    });
    connectionRef.current = connection;
    connection.start().catch((err) => console.error("Erro ao conectar ao SignalR:", err));

    return () => {
      connection.stop();
    };
  }, []);

  const handleAck = async (id: number) => {
    await alertsApi.ack(id);
    setAlerts((prev) => prev.map((a) => (a.id === id ? { ...a, acknowledged: true } : a)));
  };

  if (alerts.length === 0) {
    return <p className="empty-state">Nenhum alerta registrado ainda.</p>;
  }

  return (
    <ul className="alerts-list">
      {alerts.map((alert) => (
        <li key={alert.id} className={alert.acknowledged ? "alert-item acked" : "alert-item"}>
          <span className={`badge ${TYPE_CLASS[alert.type]}`}>{TYPE_LABELS[alert.type]}</span>
          <span className="alert-message">{alert.message}</span>
          <span className="alert-time">{new Date(alert.timestamp).toLocaleTimeString()}</span>
          {!alert.acknowledged && (
            <button className="btn btn-sm" onClick={() => handleAck(alert.id)}>
              Reconhecer
            </button>
          )}
        </li>
      ))}
    </ul>
  );
}
