export interface PositionDto {
  latitude: number;
  longitude: number;
  speed: number;
  heading: number;
  timestamp: string;
}

export interface VehicleDto {
  id: number;
  plate: string;
  model: string;
  driver: string;
  imei: string | null;
  ignitionOn: boolean | null;
  createdAt: string;
  lastPosition: PositionDto | null;
}

export interface VehicleCreateDto {
  plate: string;
  model: string;
  driver: string;
  imei?: string;
}

export interface PositionUpdatedEvent {
  vehicleId: number;
  latitude: number;
  longitude: number;
  speed: number;
  heading: number;
  timestamp: string;
}
