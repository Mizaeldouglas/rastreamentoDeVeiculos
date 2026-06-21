import { useEffect, useMemo, useRef, useState } from "react";
import { MapContainer, Marker, Polygon, Polyline, Popup, TileLayer } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { createPositionConnection } from "../services/signalr";
import type { PositionUpdatedEvent, VehicleDto } from "../types/vehicle";
import type { GeofenceDto, LatLngDto } from "../types/geofence";
import "./MapView.css";

interface Props {
  vehicles: VehicleDto[];
  geofences?: GeofenceDto[];
  historyRoute?: LatLngDto[];
  historyMarker?: LatLngDto | null;
  height?: string;
}

const vehicleIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

const historyIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  className: "history-marker-icon",
});

const DEFAULT_CENTER: [number, number] = [-23.5505, -46.6333];

export function MapView({
  vehicles,
  geofences = [],
  historyRoute = [],
  historyMarker = null,
  height = "500px",
}: Props) {
  const [positions, setPositions] = useState<Record<number, PositionUpdatedEvent>>({});
  const connectionRef = useRef<ReturnType<typeof createPositionConnection> | null>(null);

  useEffect(() => {
    const connection = createPositionConnection((event) => {
      setPositions((prev) => ({ ...prev, [event.vehicleId]: event }));
    });
    connectionRef.current = connection;
    connection.start().catch((err) => console.error("Erro ao conectar ao SignalR:", err));

    return () => {
      connection.stop();
    };
  }, []);

  const markers = useMemo(() => {
    return vehicles.map((vehicle) => {
      const live = positions[vehicle.id];
      const lat = live?.latitude ?? vehicle.lastPosition?.latitude;
      const lng = live?.longitude ?? vehicle.lastPosition?.longitude;
      if (lat === undefined || lng === undefined) return null;

      const speed = live?.speed ?? vehicle.lastPosition?.speed ?? 0;

      return (
        <Marker key={vehicle.id} position={[lat, lng]} icon={vehicleIcon}>
          <Popup>
            <strong>{vehicle.plate}</strong>
            <br />
            {vehicle.model} — {vehicle.driver}
            <br />
            {speed.toFixed(1)} km/h
          </Popup>
        </Marker>
      );
    });
  }, [vehicles, positions]);

  return (
    <div className="map-frame">
      <MapContainer
        center={DEFAULT_CENTER}
        zoom={13}
        style={{ height, width: "100%" }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        {markers}
        {historyRoute.length > 1 && (
          <Polyline
            positions={historyRoute.map((p) => [p.lat, p.lng])}
            pathOptions={{ color: "#f97316", weight: 3 }}
          />
        )}
        {historyMarker && (
          <Marker position={[historyMarker.lat, historyMarker.lng]} icon={historyIcon}>
            <Popup>Posição no histórico</Popup>
          </Marker>
        )}
        {geofences.map((geofence) => (
          <Polygon
            key={geofence.id}
            positions={geofence.points.map((p) => [p.lat, p.lng])}
            pathOptions={{ color: "#4f46e5", weight: 2, fillOpacity: 0.1 }}
          >
            <Popup>{geofence.name}</Popup>
          </Polygon>
        ))}
      </MapContainer>
    </div>
  );
}
