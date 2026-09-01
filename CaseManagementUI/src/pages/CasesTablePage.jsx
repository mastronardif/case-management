import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable from "../components/DataTable";
import { useGlobalStore } from "../context/GlobalStore";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";
import { enrichDocIdLinks } from "../utils/docIdLinks";
import { enrichRows } from "../utils/documentEnrichment";

const schemaEntry = QUERY_MAP.find((e) => e.resource === "searchCases");

export default function CasesTablePage() {
  const { urlCases } = useGlobalStore();
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const navigate = useNavigate();
  const dataTableRef = useRef(null);

  const fetchData = useCallback(async () => {
    if (!urlCases) return;
    setLoading(true);
    try {
      setError(null);
      const body = {
        action: "searchCases",
        params: {},
      };
      const res = await apiFetch(urlCases, body);

      const enriched = await enrichRows(res ?? [], schemaEntry?.enrichments);
      setRows(enrichDocIdLinks(enriched, navigate));
    } catch (err) {
      console.error("Error fetching cases:", err);
      setRows([]);
      setError("Failed to fetch cases.");
    } finally {
      setLoading(false);
    }
  }, [navigate, urlCases]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleExport = () => {
    dataTableRef.current?.exportCSV?.();
  };

  const handleNew = () => {
    navigate("/cases/new");
  };

  const actions = (schemaEntry?.actions ?? []).map((action) => ({
    label: action.label,
    onClick: (row) => {
      const safeRow = Object.fromEntries(
        Object.entries(row).filter(([, v]) => !(v && v.$$typeof))
      );
      navigate(
        action.route.replace(/\{(\w+)\}/g, (_, key) => safeRow[key] ?? ""),
        { state: { caseData: safeRow } }
      );
    },
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
        <ActionTable
          title="Cases"
          count={filteredRows.length}
          onReload={fetchData}
          loading={loading}
          onNew={handleNew}
          onExport={handleExport}
          onSearch={setSearch}
        />

        {error && <p className="text-red-500 mb-2">{error}</p>}

        {filteredRows.length > 0 ? (
          <DataTable
            ref={dataTableRef}
            rows={filteredRows}
            actions={actions}
            emptyMessage="No cases found."
          />
        ) : (
          <p>{loading ? "Loading cases..." : "No cases found."}</p>
        )}
      </div>
    </div>
  );
}
