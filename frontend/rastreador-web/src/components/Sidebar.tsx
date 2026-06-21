import { NavLink } from "react-router-dom";
import { LayoutDashboard, Truck, Map, ShieldAlert, History, Bell, X } from "lucide-react";
import "./Sidebar.css";

const NAV_ITEMS = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { to: "/veiculos", label: "Veículos", icon: Truck },
  { to: "/mapa", label: "Mapa", icon: Map },
  { to: "/geofences", label: "Geofences", icon: ShieldAlert },
  { to: "/historico", label: "Histórico", icon: History },
  { to: "/alertas", label: "Alertas", icon: Bell },
];

interface Props {
  open: boolean;
  onClose: () => void;
}

export function Sidebar({ open, onClose }: Props) {
  return (
    <>
      {open && <div className="sidebar-backdrop" onClick={onClose} />}
      <nav className={`sidebar${open ? " open" : ""}`}>
        <div className="sidebar-brand">
          <span className="sidebar-brand-icon">📍</span>
          <span className="sidebar-brand-text">Rastreador</span>
          <button className="sidebar-close" onClick={onClose} aria-label="Fechar menu">
            <X size={20} />
          </button>
        </div>
        <ul className="sidebar-nav">
          {NAV_ITEMS.map(({ to, label, icon: Icon }) => (
            <li key={to}>
              <NavLink
                to={to}
                onClick={onClose}
                className={({ isActive }) => `sidebar-link${isActive ? " active" : ""}`}
              >
                <Icon size={18} />
                <span>{label}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </>
  );
}
