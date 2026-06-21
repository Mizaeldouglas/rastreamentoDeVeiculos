import * as signalR from "@microsoft/signalr";
import type { PositionUpdatedEvent } from "../types/vehicle";
import type { AlertTriggeredEvent } from "../types/alert";
import { API_BASE_URL } from "./api";

export function createPositionConnection(
  onPositionUpdated?: (event: PositionUpdatedEvent) => void,
  onAlertTriggered?: (event: AlertTriggeredEvent) => void
): signalR.HubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/positions`)
    .withAutomaticReconnect()
    .build();

  if (onPositionUpdated) {
    connection.on("PositionUpdated", onPositionUpdated);
  }
  if (onAlertTriggered) {
    connection.on("AlertTriggered", onAlertTriggered);
  }

  return connection;
}
