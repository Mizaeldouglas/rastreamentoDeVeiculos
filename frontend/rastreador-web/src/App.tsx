import { useEffect, useState } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { getSession, logout, onUnauthorized } from "./services/auth";
import { LoginPage } from "./components/LoginPage";
import { AppLayout } from "./layouts/AppLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { VehiclesPage } from "./pages/VehiclesPage";
import { MapPage } from "./pages/MapPage";
import { GeofencesPage } from "./pages/GeofencesPage";
import { HistoryPage } from "./pages/HistoryPage";
import { AlertsPage } from "./pages/AlertsPage";
import type { AuthResponseDto } from "./types/auth";
import "./App.css";

function App() {
  const [session, setSession] = useState<AuthResponseDto | null>(() => getSession());

  useEffect(() => {
    onUnauthorized(() => setSession(null));
  }, []);

  const handleLogout = () => {
    logout();
    setSession(null);
  };

  if (!session) {
    return <LoginPage onAuthenticated={setSession} />;
  }

  return (
    <Routes>
      <Route element={<AppLayout session={session} onLogout={handleLogout} />}>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/veiculos" element={<VehiclesPage />} />
        <Route path="/mapa" element={<MapPage />} />
        <Route path="/geofences" element={<GeofencesPage />} />
        <Route path="/historico" element={<HistoryPage />} />
        <Route path="/alertas" element={<AlertsPage />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
