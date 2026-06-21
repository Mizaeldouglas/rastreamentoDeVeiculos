import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { geofencesApi, vehiclesApi } from "../services/api";
import type { VehicleDto } from "../types/vehicle";
import type { GeofenceDto } from "../types/geofence";

interface FleetDataValue {
  vehicles: VehicleDto[];
  geofences: GeofenceDto[];
  error: string | null;
  reloadVehicles: () => Promise<void>;
  reloadGeofences: () => Promise<void>;
}

const FleetDataContext = createContext<FleetDataValue | null>(null);

const POLL_INTERVAL_MS = 5000;

export function FleetDataProvider({ children }: { children: ReactNode }) {
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [geofences, setGeofences] = useState<GeofenceDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const reloadVehicles = useCallback(async () => {
    try {
      const data = await vehiclesApi.list();
      setVehicles(data);
      setError(null);
    } catch (err) {
      console.error(err);
      setError("Não foi possível conectar à API. Verifique se o backend está rodando.");
    }
  }, []);

  const reloadGeofences = useCallback(async () => {
    try {
      const data = await geofencesApi.list();
      setGeofences(data);
    } catch (err) {
      console.error(err);
    }
  }, []);

  useEffect(() => {
    reloadVehicles();
    reloadGeofences();
    const interval = setInterval(() => {
      reloadVehicles();
      reloadGeofences();
    }, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [reloadVehicles, reloadGeofences]);

  return (
    <FleetDataContext.Provider value={{ vehicles, geofences, error, reloadVehicles, reloadGeofences }}>
      {children}
    </FleetDataContext.Provider>
  );
}

export function useFleetData(): FleetDataValue {
  const ctx = useContext(FleetDataContext);
  if (!ctx) throw new Error("useFleetData deve ser usado dentro de FleetDataProvider");
  return ctx;
}
