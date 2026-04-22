// src/pages/FilesTablePage.jsx
import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useGlobalStore } from "../context/GlobalStore";
import { fetchFileList } from "../services/fileService";
import XyzTablePage from "./XyzTablePage";

// Unified action row for files
const FileActionRow = ({ row, actions }) => (
  <div className="flex gap-1">
    {actions.map((action, i) => (
      <button
        key={i}
        onClick={() => action.onClick(row)}
        className={action.className || "px-3 py-1 text-sm rounded bg-green-500 text-white hover:bg-green-600"}
      >
        {action.label}
      </button>
    ))}
  </div>
);

export default function FilesTablePage() {
  const { urlTemplates } = useGlobalStore();
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const fetchData = useCallback(async () => {
    if (!urlTemplates) return;
    setLoading(true);
    setError(null);

    try {
      const res = await fetchFileList(urlTemplates);
      const normalized = Array.isArray(res)
        ? res.map((item) => (typeof item === "string" ? { filename: item } : item))
        : res && typeof res === "object"
        ? [res]
        : [];
      setRows(normalized);
    } catch (err) {
      console.error("Error fetching files:", err);
      setRows([]);
      setError("Failed to fetch files.");
    } finally {
      setLoading(false);
    }
  }, [urlTemplates]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Row-level actions
  const rowActions = [
    {
      label: "Open",
      onClick: (row) => {
        const fileUrl = row.filename || row.file || row.name;
        if (!fileUrl) return;

        const url = fileUrl.startsWith("http") ? fileUrl : `files/${fileUrl}`;
        navigate("/viewer", { state: { fileUrl: url, title: `Viewing ${fileUrl}` } });
      },
      className: "bg-green-500 text-white hover:bg-green-600",
    },
    {
      label: "Delete",
      onClick: (row) => alert(`Delete ${row.filename || row.file || row.name} coming soon`),
      className: "bg-red-500 text-white hover:bg-red-600",
    },
  ];

  // Table-level actions
  const tableActions = [
    {
      label: loading ? "Loading..." : "Reload",
      onClick: fetchData,
      className: "bg-blue-500 text-white hover:bg-blue-600 px-3 py-1 rounded text-sm",
    },
  ];

  return (
    <div >
      <XyzTablePage
        title="Files"
        rows={rows}
        ActionRowComponent={FileActionRow}
        rowActions={rowActions}
        tableActions={tableActions}
      />

      {error && <p className="text-red-500 mt-2">{error}</p>}
    </div>
  );
}
