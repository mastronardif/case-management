import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import DataTable from "../components/DataTable";
import { fetchBillingData } from "../services/billingService";

export default function BillingTablePage() {
  const [rows, setRows] = useState([]);
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await fetchBillingData(10);
      setRows(data);
    } catch (err) {
      console.error("Error fetching billing data:", err);
      setRows([]);
      setError("Failed to fetch billing data.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleOpenCase = (row) => {
    // Navigate to CasePage with URL param and optional state
    navigate(`/billing/${row.case} (Payer: ${row.Payer})`, { state: { caseData: row } });
  };

  // Prepare rows with Action column if you want buttons, optional
  const tableRows = rows.map((row) => ({
    ...row,
    // Example: no actual button yet, but you could add View/Pay actions
    Action: (
      <button
        onClick={() => handleOpenCase(row)}
        className="px-2 py-1 text-sm bg-blue-500 text-white rounded hover:bg-blue-600"
      >
        View
      </button>
    ),
  }));

  return (
    <div className="relative min-h-screen p-6 flex flex-col items-center">
      <div className="relative w-full max-w-6xl p-6 rounded shadow bg-white mb-6">
        <h1 className="text-xl font-bold mb-4">Billing</h1>

        <div className="flex gap-2 mb-4 flex-wrap">
          <button
            onClick={fetchData}
            disabled={loading}
            className={`px-3 py-1 rounded hover:bg-blue-600 ${
              loading
                ? "bg-gray-400 cursor-not-allowed"
                : "bg-blue-500 text-white"
            }`}
          >
            {loading ? "Loading..." : "Reload"}
          </button>
        </div>

        {error && <p className="text-red-500 mb-2">{error}</p>}

        {rows.length > 0 ? (
          <DataTable rows={tableRows} />
        ) : (
          <p>
            {loading ? "Loading billing data..." : "No billing data found."}
          </p>
        )}
      </div>
    </div>
  );
}
