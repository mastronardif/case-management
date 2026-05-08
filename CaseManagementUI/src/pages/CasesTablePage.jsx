import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable22 from "../components/DataTable22";
import { useGlobalStore } from "../context/GlobalStore";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";


export default function CasesTablePage() {
  const { urlCases } = useGlobalStore();
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const navigate = useNavigate();

  const fetchData = useCallback(async () => {
    if (!urlCases) return;
    setLoading(true);
    try {
      setError(null);      
      // const res = await api.get(urlCases);
      const body = {
        "action": "searchCases",
        "params": { }
        };
      const res = await apiFetch(urlCases, body);

      setRows(res ?? []); // always safe

    } catch (err) {
      console.error("Error fetching cases:", err);
      setRows([]);
      setError("Failed to fetch cases.");
    } finally {
      setLoading(false);
    }
  }, [urlCases]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleExport = () => {
    console.log("Export clicked");
    // TODO: implement CSV/Excel export
    alert("Export CSV not implemented yet");
  };

  const handleNew = () => {
    console.log("New clicked");
    // TODO: navigate to new case form
    navigate("/cases/new");
  };

  const schemaEntry = QUERY_MAP.find((e) => e.resource === "searchCases");
  const actions = (schemaEntry?.actions ?? []).map((action) => ({
    label: action.label,
    onClick: (row) =>
      navigate(
        action.route.replace(/\{(\w+)\}/g, (_, key) => row[key] ?? ""),
        { state: { caseData: row } }
      ),
  }));

  const filteredRows = rows.filter((row) =>
    JSON.stringify(row).toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="relative min-h-screen p-6 flex justify-center">
      <div className="relative w-full max-w-6xl p-6 rounded shadow bg-white">
        <ActionTable
          title="Cases"
          onReload={fetchData}
          loading={loading}
          onNew={handleNew}
          onExport={handleExport}
          onSearch={setSearch}
        />

        {error && <p className="text-red-500 mb-2">{error}</p>}

        {filteredRows.length > 0 ? (
          <DataTable22 rows={filteredRows} actions={actions} />
        ) : (
          <p>{loading ? "Loading cases..." : "No cases found."}</p>
        )}
      </div>
    </div>
  );
}
