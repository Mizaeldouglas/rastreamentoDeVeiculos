import { useState } from "react";
import type { VehicleCreateDto } from "../types/vehicle";
import "./VehicleForm.css";

interface Props {
  onSubmit: (dto: VehicleCreateDto) => Promise<void>;
}

export function VehicleForm({ onSubmit }: Props) {
  const [plate, setPlate] = useState("");
  const [model, setModel] = useState("");
  const [driver, setDriver] = useState("");
  const [imei, setImei] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!plate.trim()) return;

    setSubmitting(true);
    try {
      await onSubmit({ plate, model, driver, imei: imei || undefined });
      setPlate("");
      setModel("");
      setDriver("");
      setImei("");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="vehicle-form">
      <label className="field">
        <span className="field-label">Placa</span>
        <input
          placeholder="Placa"
          value={plate}
          onChange={(e) => setPlate(e.target.value)}
          required
        />
      </label>
      <label className="field">
        <span className="field-label">Modelo</span>
        <input
          placeholder="Modelo"
          value={model}
          onChange={(e) => setModel(e.target.value)}
        />
      </label>
      <label className="field">
        <span className="field-label">Motorista</span>
        <input
          placeholder="Motorista"
          value={driver}
          onChange={(e) => setDriver(e.target.value)}
        />
      </label>
      <label className="field">
        <span className="field-label">IMEI (opcional)</span>
        <input
          placeholder="IMEI do rastreador (opcional)"
          value={imei}
          onChange={(e) => setImei(e.target.value)}
        />
      </label>
      <div className="field-actions">
        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? "Salvando..." : "Adicionar veículo"}
        </button>
      </div>
    </form>
  );
}
