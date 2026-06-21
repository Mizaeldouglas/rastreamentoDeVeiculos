import { useFleetData } from "../context/FleetDataContext";
import { VehicleForm } from "../components/VehicleForm";
import { VehicleList } from "../components/VehicleList";
import { vehiclesApi } from "../services/api";
import type { VehicleCreateDto } from "../types/vehicle";

export function VehiclesPage() {
  const { vehicles, error, reloadVehicles } = useFleetData();

  const handleCreate = async (dto: VehicleCreateDto) => {
    await vehiclesApi.create(dto);
    await reloadVehicles();
  };

  const handleDelete = async (id: number) => {
    await vehiclesApi.remove(id);
    await reloadVehicles();
  };

  return (
    <div className="page">
      <div>
        <h1 className="page-title">Veículos</h1>
        <p className="page-subtitle">Cadastro e gerenciamento da frota</p>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <section className="card">
        <h2 className="section-title">Novo veículo</h2>
        <VehicleForm onSubmit={handleCreate} />
      </section>

      <section className="card">
        <h2 className="section-title">Veículos cadastrados</h2>
        <VehicleList vehicles={vehicles} onDelete={handleDelete} />
      </section>
    </div>
  );
}
