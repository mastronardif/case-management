import { useCallback, useEffect, useRef, useState } from "react";
import ActionTable from "../components/ActionTable";
import DataTable from "../components/DataTable";
import { useGlobalStore } from "../context/GlobalStore";
import { apiFetch } from "../services/apiFetch";

export default function TableFromUrl() {
  const { url, body } = useGlobalStore();
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const dataTableRef = useRef(null);
  const fetchPromiseRef = useRef(null);

  const fetchData = useCallback(async () => {
    if (!url) return;
    if (fetchPromiseRef.current) return fetchPromiseRef.current;

    setLoading(true);

    fetchPromiseRef.current = (async () => {
      try {
        setError(null);
        const result = body ? await apiFetch(url, body) : await apiFetch(url);
        setRows(Array.isArray(result) ? result : result ? [result] : []);
      } catch (err) {
        console.error(err);
        setRows([]);
        setError("Failed to fetch data.");
      } finally {
        fetchPromiseRef.current = null;
        setLoading(false);
      }
    })();

    return fetchPromiseRef.current;
  }, [url, body]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleExport = () => {
    dataTableRef.current?.exportCSV?.();
  };

  const filteredRows = rows.filter((row) => {
    const normalizedSearch = search.trim().toLowerCase();
    if (!normalizedSearch) return true;

    return Object.values(row ?? {}).some((value) => {
      if (value === null || value === undefined) return false;
      if (typeof value === "object") {
        return JSON.stringify(value).toLowerCase().includes(normalizedSearch);
      }
      return String(value).toLowerCase().includes(normalizedSearch);
    });
  });

  return (
    <div className="p-6">
      <div className="flex items-center gap-2 mb-4 flex-wrap">
        <ActionTable
          title={url ? `Data Table (${url})` : "Data Table"}
          count={filteredRows.length}
          onReload={fetchData}
          onExport={handleExport}
          onSearch={setSearch}
          loading={loading}
        />
      </div>

      {error && <p className="text-red-500 mb-2">{error}</p>}

      {loading && rows.length === 0 ? (
        <p>Loading data...</p>
      ) : filteredRows.length > 0 ? (
        <DataTable
          ref={dataTableRef}
          rows={filteredRows}
          emptyMessage="No data loaded yet."
        />
      ) : (
        <p>No data loaded yet.</p>
      )}
    </div>
  );
}
