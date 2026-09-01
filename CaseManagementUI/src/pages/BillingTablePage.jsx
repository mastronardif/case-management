import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable from "../components/DataTable";
import { useGlobalStore } from "../context/GlobalStore";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";

const greyBtn =
  "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0";

export default function BillingTablePage() {
  const { urlCases } = useGlobalStore();
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const navigate = useNavigate();
  const dataTableRef = useRef(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);
      const res = await apiFetch(urlCases, { action: "getInvoices", params: {} });
      setRows(res ?? []);
    } catch (err) {
      console.error("Error fetching invoices:", err);
      setRows([]);
      setError("Failed to fetch invoices.");
    } finally {
      setLoading(false);
    }
  }, [urlCases]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleExport = () => {
    dataTableRef.current?.exportCSV?.();
  };

  const schemaEntry = QUERY_MAP.find((e) => e.resource === "getInvoices");
  const actions = (schemaEntry?.actions ?? []).map((action) => ({
    label: action.label,
    onClick: (row) =>
      navigate(
        action.route.replace(/\{(\w+)\}/g, (_, key) => row[key] ?? ""),
        { state: { invoiceData: row } }
      ),
  }));

  const filteredRows = rows.filter((row) => {
    const normalizedSearch = search.trim().toLowerCase();
    if (!normalizedSearch) return true;

    return Object.values(row ?? {}).some((value) => {
      if (value === null || value === undefined) return false;
      if (typeof value === "object" && !value.$$typeof) {
        return JSON.stringify(value).toLowerCase().includes(normalizedSearch);
      }
      return String(value).toLowerCase().includes(normalizedSearch);
    });
  });

  return (
    <div className="relative min-h-screen p-6 flex justify-center">
      <div className="relative w-full max-w-6xl p-6 rounded shadow bg-white">
        <div className="flex items-center gap-2 mb-4 flex-wrap">
          <ActionTable
            title="Billing"
            count={filteredRows.length}
            onReload={fetchData}
            onExport={handleExport}
            loading={loading}
            onSearch={setSearch}
            buttonClass={greyBtn}
          />
          <button onClick={() => alert("F1")} className={greyBtn}>F1</button>
          <button onClick={() => alert("F2")} className={greyBtn}>F2</button>
          <button onClick={() => alert("F3")} className={greyBtn}>F3</button>
        </div>

        {error && <p className="text-red-500 mb-2">{error}</p>}

        {filteredRows.length > 0 ? (
          <DataTable
            ref={dataTableRef}
            rows={filteredRows}
            actions={actions}
            emptyMessage="No invoices found."
          />
        ) : (
          <p>{loading ? "Loading invoices..." : "No invoices found."}</p>
        )}
      </div>
    </div>
  );
}
