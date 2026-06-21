export interface LatLngDto {
  lat: number;
  lng: number;
}

export interface GeofenceDto {
  id: number;
  name: string;
  vehicleId: number | null;
  points: LatLngDto[];
  alertOnEnter: boolean;
  alertOnExit: boolean;
  createdAt: string;
}

export interface GeofenceCreateDto {
  name: string;
  vehicleId?: number | null;
  points: LatLngDto[];
  alertOnEnter: boolean;
  alertOnExit: boolean;
}
