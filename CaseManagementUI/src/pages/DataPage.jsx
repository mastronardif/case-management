import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable22 from "../components/DataTable22";
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

  const allTableActions = [...getTableActions(resource), ...tableActions];

  const schemaEntry = QUERY_MAP.find((e) => e.resource === resource);
  const urlContext = type && id ? { [type]: id } : {};
  const actions = (schemaEntry?.actions ?? []).map((action) => ({
    label: action.label,
    onClick: (row) =>
      navigate(
        action.route.replace(/\{(\w+)\}/g, (_, key) => row[key] ?? urlContext[key] ?? ""),
        { state: { ...row, ...urlContext, caseData: row } }
      ),
  }));

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
    try {
      setError(null);
      const body = resolvedRequest?.action
        ? { action: resolvedRequest.action, params: resolvedRequest.params || {} }
        : resolvedRequest?.body || null;
      const result = await apiFetch(resolvedRequest.url, body);
      const data = result?.data ?? result ?? [];
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

  const handleExport = () => {
    if (!rows.length) return;
    const cols = Object.keys(rows[0]);
    const csv = [cols.join(","), ...rows.map((r) =>
      cols.map((c) => {
        const v = r[c] ?? "";
        const s = typeof v === "object" ? JSON.stringify(v) : String(v);
        return s.includes(",") || s.includes('"') ? `"${s.replace(/"/g, '""')}"` : s;
      }).join(",")
    )].join("\n");
    const a = document.createElement("a");
    a.href = URL.createObjectURL(new Blob([csv], { type: "text/csv" }));
    a.download = `${resource ?? "export"}.csv`;
    a.click();
  };

  const resolvedTitle = title ?? `${resource ?? ""}${type ? ` / ${type}` : ""}${id ? ` / ${id}` : ""}`;

  const filteredRows = rows.filter((row) =>
    JSON.stringify(row).toLowerCase().includes(search.toLowerCase())
  );

  const btnClass = "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed";

  return (
    <div className="p-6">
      <div className="flex items-center gap-2 mb-4 flex-wrap">
        <ActionTable
          title={resolvedTitle}
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

      {filteredRows.length > 0 ? (
        <DataTable22 rows={filteredRows} actions={actions} />
      ) : (
        <p>{loading ? "Loading..." : "No data found."}</p>
      )}

      <div className="mt-4 text-sm text-gray-500">{rows.length} rows</div>
    </div>
  );
}
