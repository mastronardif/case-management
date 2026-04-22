import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable from "../components/DataTable";
import { useGlobalStore } from "../context/GlobalStore";
import api from "../services/http";


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
      const res = await api.get(urlCases);
      let normalized = [];
      if (res.data?.data) {
        normalized = Array.isArray(res.data.data)
          ? res.data.data
          : [res.data.data];
      } else if (Array.isArray(res.data)) {
        normalized = res.data;
      } else if (res.data && typeof res.data === "object") {
        normalized = [res.data];
      }
      setRows(normalized);
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

  const handleOpenCase = (row) => {
    // Navigate to CasePage with URL param and optional state
    navigate(`/cases/${row.id}`, { state: { caseData: row } });
  };

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
          <DataTable
            rows={filteredRows.map((row) => ({
              ...row,
              Action: (
                <button
                  onClick={() => handleOpenCase(row)}
                  className="px-2 py-1 text-sm bg-green-500 text-white rounded hover:bg-green-600"
                >
                  Open
                </button>
              ),
            }))}
          />
        ) : (
          <p>{loading ? "Loading cases..." : "No cases found."}</p>
        )}
      </div>
    </div>
  );
}
