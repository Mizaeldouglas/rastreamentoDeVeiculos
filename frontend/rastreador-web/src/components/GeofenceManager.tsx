import { useState } from "react";
import { geofencesApi } from "../services/api";
import type { GeofenceDto } from "../types/geofence";
import type { VehicleDto } from "../types/vehicle";
import "./GeofenceManager.css";

interface Props {
  vehicles: VehicleDto[];
  geofences: GeofenceDto[];
  onChange: () => void;
}

export function GeofenceManager({ vehicles, geofences, onChange }: Props) {
  const [name, setName] = useState("");
  const [vehicleId, setVehicleId] = useState<string>("");
  const [north, setNorth] = useState("");
  const [south, setSouth] = useState("");
  const [east, setEast] = useState("");
  const [west, setWest] = useState("");
  const [alertOnEnter, setAlertOnEnter] = useState(true);
  const [alertOnExit, setAlertOnExit] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const n = parseFloat(north);
    const s = parseFloat(south);
    const ea = parseFloat(east);
    const w = parseFloat(west);
    if (!name.trim() || [n, s, ea, w].some(Number.isNaN)) return;

    setSubmitting(true);
    try {
      await geofencesApi.create({
        name,
        vehicleId: vehicleId ? Number(vehicleId) : null,
        alertOnEnter,
        alertOnExit,
        points: [
          { lat: n, lng: w },
          { lat: n, lng: ea },
          { lat: s, lng: ea },
          { lat: s, lng: w },
        ],
      });
      setName("");
      setNorth("");
      setSouth("");
      setEast("");
      setWest("");
      onChange();
    } finally {
      setSubmitting(false);
    }
  };

  const handleRemove = async (id: number) => {
    await geofencesApi.remove(id);
    onChange();
  };

  return (
    <div>
      <form onSubmit={handleSubmit} className="geofence-form">
        <label className="field">
          <span className="field-label">Nome</span>
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>

        <label className="field">
          <span className="field-label">Veículo</span>
          <select value={vehicleId} onChange={(e) => setVehicleId(e.target.value)}>
            <option value="">Todos os veículos</option>
            {vehicles.map((v) => (
              <option key={v.id} value={v.id}>
                {v.plate}
              </option>
            ))}
          </select>
        </label>

        <div className="bbox-grid">
          <label className="field">
            <span className="field-label">Norte (lat)</span>
            <input value={north} onChange={(e) => setNorth(e.target.value)} required />
          </label>
          <label className="field">
            <span className="field-label">Sul (lat)</span>
            <input value={south} onChange={(e) => setSouth(e.target.value)} required />
          </label>
          <label className="field">
            <span className="field-label">Leste (lng)</span>
            <input value={east} onChange={(e) => setEast(e.target.value)} required />
          </label>
          <label className="field">
            <span className="field-label">Oeste (lng)</span>
            <input value={west} onChange={(e) => setWest(e.target.value)} required />
          </label>
        </div>

        <div className="checkbox-row">
          <label>
            <input
              type="checkbox"
              checked={alertOnEnter}
              onChange={(e) => setAlertOnEnter(e.target.checked)}
            />
            Alertar ao entrar
          </label>
          <label>
            <input
              type="checkbox"
              checked={alertOnExit}
              onChange={(e) => setAlertOnExit(e.target.checked)}
            />
            Alertar ao sair
          </label>
        </div>

        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? "Salvando..." : "Criar geofence"}
        </button>
      </form>

      {geofences.length === 0 ? (
        <p className="empty-state">Nenhuma geofence cadastrada ainda.</p>
      ) : (
        <ul className="geofence-list">
          {geofences.map((g) => (
            <li key={g.id} className="geofence-item">
              <span>
                {g.name} {g.vehicleId ? `(veículo #${g.vehicleId})` : "(todos os veículos)"}
              </span>
              <button className="btn btn-danger btn-sm" onClick={() => handleRemove(g.id)}>
                ✕ Remover
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
