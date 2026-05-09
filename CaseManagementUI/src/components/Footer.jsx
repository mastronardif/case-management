import { useEffect, useState } from "react";
import { apiFetch } from "../services/apiFetch";

const VERSION = "1.0.0";
const ENV = import.meta.env.MODE;

export default function Footer() {
  const [status, setStatus] = useState("checking");

  useEffect(() => {
    const check = async () => {
      try {
        await apiFetch("/api/corqs", { action: "getServerTime", params: {} });
        setStatus("connected");
      } catch {
        setStatus("disconnected");
      }
    };

    check();
    const interval = setInterval(check, 30000);
    return () => clearInterval(interval);
  }, []);

  const statusColor =
    status === "connected" ? "text-green-400" :
    status === "disconnected" ? "text-red-400" :
    "text-yellow-400";

  const statusLabel =
    status === "connected" ? "Connected" :
    status === "disconnected" ? "Disconnected" :
    "Checking...";

  return (
    <footer className="h-7 bg-gray-800 text-gray-400 text-xs flex items-center justify-between px-4 flex-shrink-0">
      <span>© {new Date().getFullYear()} Case Management</span>
      <div className="flex items-center gap-4">
        <span className={statusColor}>{statusLabel}</span>
        <span>{ENV}</span>
        <span>v{VERSION}</span>
      </div>
    </footer>
  );
}
