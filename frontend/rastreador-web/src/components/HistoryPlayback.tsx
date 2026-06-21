import { useEffect, useState } from "react";
import { vehiclesApi } from "../services/api";
import type { PositionDto, VehicleDto } from "../types/vehicle";
import type { LatLngDto } from "../types/geofence";
import "./HistoryPlayback.css";

interface Props {
  vehicles: VehicleDto[];
  onChange: (route: LatLngDto[], marker: LatLngDto | null) => void;
}

function toDateTimeLocal(date: Date): string {
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

const SPEED_OPTIONS = [1, 2, 5];
const BASE_INTERVAL_MS = 500;

export function HistoryPlayback({ vehicles, onChange }: Props) {
  const now = new Date();
  const dayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);

  const [vehicleId, setVehicleId] = useState("");
  const [from, setFrom] = useState(toDateTimeLocal(dayAgo));
  const [to, setTo] = useState(toDateTimeLocal(now));
  const [positions, setPositions] = useState<PositionDto[]>([]);
  const [index, setIndex] = useState(0);
  const [playing, setPlaying] = useState(false);
  const [speed, setSpeed] = useState(1);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    const route = positions.map((p) => ({ lat: p.latitude, lng: p.longitude }));
    const marker = positions[index] ? { lat: positions[index].latitude, lng: positions[index].longitude } : null;
    onChange(route, marker);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [positions, index]);

  useEffect(() => {
    if (!playing || positions.length === 0) return;

    const interval = setInterval(() => {
      setIndex((prev) => {
        if (prev >= positions.length - 1) {
          setPlaying(false);
          return prev;
        }
        return prev + 1;
      });
    }, BASE_INTERVAL_MS / speed);

    return () => clearInterval(interval);
  }, [playing, positions.length, speed]);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!vehicleId) return;

    setLoading(true);
    setPlaying(false);
    try {
      const data = await vehiclesApi.history(
        Number(vehicleId),
        new Date(from).toISOString(),
        new Date(to).toISOString()
      );
      setPositions(data);
      setIndex(0);
    } finally {
      setLoading(false);
    }
  };

  const current = positions[index];
  const selectedVehicle = vehicles.find((v) => v.id === Number(vehicleId));

  const handleExportExcel = async () => {
    if (!selectedVehicle) return;
    setExporting(true);
    try {
      await vehiclesApi.downloadHistoryExcel(
        selectedVehicle.id,
        selectedVehicle.plate,
        new Date(from).toISOString(),
        new Date(to).toISOString()
      );
    } finally {
      setExporting(false);
    }
  };

  return (
    <div>
      <form onSubmit={handleSearch} className="history-form">
        <label className="field">
          <span className="field-label">Veículo</span>
          <select value={vehicleId} onChange={(e) => setVehicleId(e.target.value)} required>
            <option value="">Selecione...</option>
            {vehicles.map((v) => (
              <option key={v.id} value={v.id}>
                {v.plate}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field-label">De</span>
          <input type="datetime-local" value={from} onChange={(e) => setFrom(e.target.value)} required />
        </label>
        <label className="field">
          <span className="field-label">Até</span>
          <input type="datetime-local" value={to} onChange={(e) => setTo(e.target.value)} required />
        </label>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Buscando..." : "Buscar"}
        </button>
        {positions.length > 0 && (
          <button type="button" className="btn btn-sm" onClick={handleExportExcel} disabled={exporting}>
            {exporting ? "Gerando..." : "📊 Exportar Excel"}
          </button>
        )}
      </form>

      {positions.length === 0 ? (
        <p className="empty-state">Nenhuma posição encontrada para o período selecionado.</p>
      ) : (
        <div className="playback-controls">
          <button type="button" className="btn btn-sm" onClick={() => setPlaying((p) => !p)}>
            {playing ? "⏸ Pausar" : "▶ Reproduzir"}
          </button>

          <input
            type="range"
            min={0}
            max={positions.length - 1}
            value={index}
            onChange={(e) => {
              setPlaying(false);
              setIndex(Number(e.target.value));
            }}
            className="playback-slider"
          />

          <select value={speed} onChange={(e) => setSpeed(Number(e.target.value))}>
            {SPEED_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {s}x
              </option>
            ))}
          </select>

          {current && (
            <span className="playback-info">
              {new Date(current.timestamp).toLocaleString()} — {current.speed.toFixed(1)} km/h ({index + 1}/
              {positions.length})
            </span>
          )}
        </div>
      )}
    </div>
  );
}
