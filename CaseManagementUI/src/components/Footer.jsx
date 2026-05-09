import { useEffect, useState } from "react";
import { getConnectionStatus, subscribeConnection } from "../utils/connectionStore";

const VERSION = "1.0.0";
const ENV = import.meta.env.MODE;

export default function Footer() {
  const [status, setStatus] = useState(getConnectionStatus);

  useEffect(() => subscribeConnection(setStatus), []);

  const statusColor = status === "connected" ? "text-green-400" : "text-red-400";
  const statusLabel = status === "connected" ? "Connected" : "Disconnected";

  return (
    <footer className="h-7 bg-gray-800 text-gray-400 text-xs flex items-center justify-between px-4 flex-shrink-0 min-w-0">
      <span className="truncate min-w-0 mr-4">© {new Date().getFullYear()} Case Management</span>
      <div className="flex items-center gap-4 flex-shrink-0">
        <span className={statusColor}>{statusLabel}</span>
        <span className="hidden sm:inline">{ENV}</span>
        <span>v{VERSION}</span>
      </div>
    </footer>
  );
}
