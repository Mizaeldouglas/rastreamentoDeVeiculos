import axios from "axios";
import { API_BASE_URL } from "./config";
import { getToken, triggerUnauthorized } from "./auth";
import type { PositionDto, VehicleCreateDto, VehicleDto } from "../types/vehicle";
import type { AlertDto } from "../types/alert";
import type { GeofenceCreateDto, GeofenceDto } from "../types/geofence";

export { API_BASE_URL };

const client = axios.create({ baseURL: API_BASE_URL });

client.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      triggerUnauthorized();
    }
    return Promise.reject(error);
  }
);

export const vehiclesApi = {
  list: () => client.get<VehicleDto[]>("/api/vehicles").then((res) => res.data),
  create: (dto: VehicleCreateDto) =>
    client.post<VehicleDto>("/api/vehicles", dto).then((res) => res.data),
  update: (id: number, dto: VehicleCreateDto) =>
    client.put(`/api/vehicles/${id}`, dto).then((res) => res.data),
  remove: (id: number) => client.delete(`/api/vehicles/${id}`).then((res) => res.data),
  history: (id: number, from?: string, to?: string) =>
    client
      .get<PositionDto[]>(`/api/vehicles/${id}/history`, { params: { from, to } })
      .then((res) => res.data),
};

export const alertsApi = {
  list: (vehicleId?: number) =>
    client
      .get<AlertDto[]>("/api/alerts", { params: vehicleId ? { vehicleId } : undefined })
      .then((res) => res.data),
  ack: (id: number) => client.post(`/api/alerts/${id}/ack`).then((res) => res.data),
};

export const geofencesApi = {
  list: () => client.get<GeofenceDto[]>("/api/geofences").then((res) => res.data),
  create: (dto: GeofenceCreateDto) =>
    client.post<GeofenceDto>("/api/geofences", dto).then((res) => res.data),
  remove: (id: number) => client.delete(`/api/geofences/${id}`).then((res) => res.data),
};
