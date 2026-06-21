import { useEffect, useMemo, useRef, useState } from "react";
import { MapContainer, Marker, Polygon, Popup, TileLayer } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { createPositionConnection } from "../services/signalr";
import type { PositionUpdatedEvent, VehicleDto } from "../types/vehicle";
import type { GeofenceDto } from "../types/geofence";
import "./MapView.css";

interface Props {
  vehicles: VehicleDto[];
  geofences?: GeofenceDto[];
}

const vehicleIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

const DEFAULT_CENTER: [number, number] = [-23.5505, -46.6333];

export function MapView({ vehicles, geofences = [] }: Props) {
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
        style={{ height: "500px", width: "100%" }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        {markers}
        {geofences.map((geofence) => (
          <Polygon
            key={geofence.id}
            positions={geofence.points.map((p) => [p.lat, p.lng])}
            pathOptions={{ color: "#2563eb", weight: 2, fillOpacity: 0.1 }}
          >
            <Popup>{geofence.name}</Popup>
          </Polygon>
        ))}
      </MapContainer>
    </div>
  );
}
