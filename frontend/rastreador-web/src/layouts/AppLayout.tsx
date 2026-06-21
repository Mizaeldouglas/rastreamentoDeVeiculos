import { useState } from "react";
import { Outlet } from "react-router-dom";
import { Bell, BellRing, LogOut } from "lucide-react";
import { Sidebar } from "../components/Sidebar";
import { FleetDataProvider } from "../context/FleetDataContext";
import { enablePushNotifications } from "../services/push";
import type { AuthResponseDto } from "../types/auth";
import "./AppLayout.css";

interface Props {
  session: AuthResponseDto;
  onLogout: () => void;
}

export function AppLayout({ session, onLogout }: Props) {
  const [pushStatus, setPushStatus] = useState<"idle" | "enabling" | "enabled" | "error">("idle");

  const handleEnablePush = async () => {
    setPushStatus("enabling");
    try {
      await enablePushNotifications();
      setPushStatus("enabled");
    } catch (err) {
      console.error(err);
      setPushStatus("error");
    }
  };

  return (
    <FleetDataProvider>
      <div className="app-layout">
        <Sidebar />
        <div className="app-main">
          <header className="topbar">
            <p className="topbar-company">{session.companyName}</p>
            <div className="topbar-actions">
              <button
                className="btn btn-sm"
                onClick={handleEnablePush}
                disabled={pushStatus === "enabling" || pushStatus === "enabled"}
              >
                {pushStatus === "enabled" ? <BellRing size={16} /> : <Bell size={16} />}
                {pushStatus === "enabled"
                  ? "Notificações ativas"
                  : pushStatus === "enabling"
                  ? "Ativando..."
                  : "Ativar notificações"}
              </button>
              <button className="btn btn-sm" onClick={onLogout}>
                <LogOut size={16} />
                Sair
              </button>
            </div>
          </header>

          {pushStatus === "error" && (
            <div className="alert alert-error">
              Não foi possível ativar as notificações. Verifique a permissão do navegador.
            </div>
          )}

          <main className="app-content">
            <Outlet />
          </main>
        </div>
      </div>
    </FleetDataProvider>
  );
}
