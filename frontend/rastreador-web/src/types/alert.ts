export type AlertType = "GeofenceEnter" | "GeofenceExit" | "SpeedLimitExceeded";

export interface AlertDto {
  id: number;
  vehicleId: number;
  type: AlertType;
  message: string;
  latitude: number;
  longitude: number;
  timestamp: string;
  acknowledged: boolean;
}

export type AlertTriggeredEvent = AlertDto;
