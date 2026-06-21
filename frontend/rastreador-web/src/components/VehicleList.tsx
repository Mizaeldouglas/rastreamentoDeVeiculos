import type { VehicleDto } from "../types/vehicle";
import "./VehicleList.css";

interface Props {
  vehicles: VehicleDto[];
  onDelete: (id: number) => void;
}

export function VehicleList({ vehicles, onDelete }: Props) {
  if (vehicles.length === 0) {
    return <p className="empty-state">Nenhum veículo cadastrado ainda.</p>;
  }

  return (
    <table className="vehicle-table">
      <thead>
        <tr>
          <th>Placa</th>
          <th>Modelo</th>
          <th>Motorista</th>
          <th>Origem</th>
          <th>Velocidade</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {vehicles.map((v) => (
          <tr key={v.id}>
            <td>{v.plate}</td>
            <td>{v.model}</td>
            <td>{v.driver}</td>
            <td>
              <span className={v.imei ? "badge badge-real" : "badge badge-sim"}>
                {v.imei ? `Dispositivo real (${v.imei})` : "Simulado"}
              </span>
            </td>
            <td>{v.lastPosition ? `${v.lastPosition.speed} km/h` : "-"}</td>
            <td>
              <button className="btn btn-danger btn-sm" onClick={() => onDelete(v.id)}>
                ✕ Remover
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
