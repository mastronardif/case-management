import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable from "../components/DataTable";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";
import { buildQuery } from "../utils/routeToQuery";
import { getTableActions } from "../utils/tableActionStore";

export default function DataPage({
  title,
  request,
  tableActions = [],
}) {
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState("");

  const navigate = useNavigate();
  const { resource, type, id } = useParams();
  const { state } = useLocation();
  const dataTableRef = useRef(null);

  const allTableActions = [...getTableActions(resource), ...tableActions];
  const schemaEntry = QUERY_MAP.find((e) => e.resource === resource);

  const actions = useMemo(() => {
    const urlContext = type && id ? { [type]: id } : {};
    return (schemaEntry?.actions ?? []).map((action) => ({
      label: action.label,
      onClick: (row) =>
        navigate(
          action.route.replace(/\{(\w+)\}/g, (_, key) => row[key] ?? urlContext[key] ?? ""),
          { state: { ...row, ...urlContext, caseData: row } }
        ),
    }));
  }, [schemaEntry, navigate, type, id]);

  const resolvedRequest = useMemo(() => {
    if (request) return request;
    const query = buildQuery(resource, type, id, state ?? {});
    if (!query) return null;
    return { url: "/api/corqs", ...query };
  }, [request, resource, type, id, state]);

  const fetchData = useCallback(async () => {
    if (!resolvedRequest?.url) {
      console.warn("No request available");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const body = resolvedRequest?.action
        ? { action: resolvedRequest.action, params: resolvedRequest.params || {} }
        : resolvedRequest?.body || null;

      const result = await apiFetch(resolvedRequest.url, body);
      const normalized = Array.isArray(result) ? result : result ? [result] : [];
      setRows(normalized);
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

  const handleExport = () => {
    dataTableRef.current?.exportCSV?.();
  };

  const resolvedTitle =
    title ?? `${resource ?? ""}${type ? ` / ${type}` : ""}${id ? ` / ${id}` : ""}`;

  const filteredRows = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    if (!normalizedSearch) return rows;

    return rows.filter((row) =>
      Object.values(row ?? {}).some((value) => {
        if (value === null || value === undefined) return false;
        if (typeof value === "object" && !React.isValidElement(value)) {
          return JSON.stringify(value).toLowerCase().includes(normalizedSearch);
        }
        return String(value).toLowerCase().includes(normalizedSearch);
      })
    );
  }, [rows, search]);

  const btnClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed";

  return (
    <div className="p-6">
      <div className="flex items-center gap-2 mb-4 flex-wrap">
        <ActionTable
          title={resolvedTitle}
          count={filteredRows.length}
          onReload={fetchData}
          onExport={handleExport}
          onSearch={setSearch}
          loading={loading}
          buttonClass={btnClass}
        />
        {allTableActions.map((a, i) => (
          <button key={i} onClick={a.onClick} className={a.className ?? btnClass}>
            {a.label}
          </button>
        ))}
      </div>

      {error && <p className="text-red-500 mb-2">{error}</p>}

      {loading && rows.length === 0 ? (
        <p>Loading...</p>
      ) : filteredRows.length > 0 ? (
        <DataTable
          ref={dataTableRef}
          rows={filteredRows}
          actions={actions}
          emptyMessage={"No data found."}
        />
      ) : (
        <p>No data found.</p>
      )}

      <div className="mt-4 text-sm text-gray-500">{rows.length} rows</div>
    </div>
  );
}
