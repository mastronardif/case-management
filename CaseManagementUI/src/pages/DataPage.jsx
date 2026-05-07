import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useParams } from "react-router-dom";
import DataTable22 from "../components/DataTable22";
import { apiFetch } from "../services/apiFetch";
import { buildQuery } from "../utils/routeToQuery";

export default function DataPage({
  title = "Data",
  request,
  rowActions = [],
  tableActions = [],
}) {
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const { resource, type, id } = useParams();
  const { state } = useLocation();

  const resolvedRequest = useMemo(() => {
    if (request) return request;

    const query = buildQuery(resource, type, id, state ?? {});

    if (!query) return null;

    return {
      url: "/api/corqs",
      ...query,
    };
  }, [request, resource, type, id, state]);

  const fetchData = useCallback(async () => {
    if (!resolvedRequest?.url) {
      console.warn("No request available");
      return;
    }

    setLoading(true);

    try {
      setError(null);

      const body =
        resolvedRequest?.action
          ? {
              action: resolvedRequest.action,
              params: resolvedRequest.params || {},
            }
          : resolvedRequest?.body || null;

      const result = await apiFetch(resolvedRequest.url, body);

      const data =
        result?.data ?? result ?? [];

      setRows(Array.isArray(data) ? data : [data]);

    } catch (err) {
      console.error("DataPage error:", err);
      setRows([]);
      setError("Failed to load data.");
    } finally {
      setLoading(false);
    }
  }, [resolvedRequest]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // 🔹 Add row actions
  const rowsWithActions = rows.map((row) => ({
    ...row,
    Action:
      rowActions.length > 0 ? (
        <div className="flex gap-1">
          {rowActions.map((a, i) => (
            <button
              key={i}
              onClick={() => a.onClick(row)}
              className={a.className}
            >
              {a.label}
            </button>
          ))}
        </div>
      ) : undefined,
  }));

  return (
    <div className="p-6">
      {/* HEADER */}
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-xl font-bold">
          {title || `${resource || ""} ${type || ""}`}
        </h1>

        <div className="flex gap-2">
          {tableActions.map((a, i) => (
            <button
              key={i}
              onClick={a.onClick}
              className={a.className}
            >
              {a.label}
            </button>
          ))}
        </div>
      </div>

      {/* ERROR */}
      {error && <p className="text-red-500 mb-2">{error}</p>}

      {/* TABLE */}
      {rowsWithActions.length > 0 ? (
        <DataTable22 rows={rowsWithActions} />
      ) : (
        <p>{loading ? "Loading..." : "No data found."}</p>
      )}

      {/* FOOTER */}
      <div className="mt-4 text-sm text-gray-500">
        {rows.length} rows
      </div>
    </div>
  );
}