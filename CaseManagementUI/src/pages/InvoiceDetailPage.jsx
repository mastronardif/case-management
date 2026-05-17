import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/http";

export default function InvoiceDetailPage() {
  const { invoiceId } = useParams();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      setError(null);
      const res = await api.post("/api/corqs", {
        action: "getInvoiceDetails",
        params: { invoiceId: Number(invoiceId) },
      });
      setData(res.data?.data ?? res.data);
    } catch (err) {
      console.error("Error fetching invoice details:", err);
      setError("Failed to load invoice details.");
    } finally {
      setLoading(false);
    }
  }, [invoiceId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <h1 className="text-xl font-bold mb-4">Invoice {invoiceId}</h1>

      {error && <p className="text-red-500 mb-2">{error}</p>}
      {loading && <p>Loading...</p>}

      {data && (
        <pre className="bg-gray-50 border border-gray-200 rounded p-4 text-sm overflow-auto">
          {JSON.stringify(data, null, 2)}
        </pre>
      )}
    </div>
  );
}
