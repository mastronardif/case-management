import { useEffect, useRef, useState } from "react";
import { useGlobalStore } from "../context/GlobalStore";
import api from "../services/http";

const renderValue = (val) => {
  if (val === null || val === undefined) return "";
  if (typeof val === "object") {
    return Object.entries(val)
      .map(([k, v]) => `${k}: ${renderValue(v)}`)
      .join(", ");
  }
  return val.toString();
};

export default function TableFromUrl() {
  const { url } = useGlobalStore();
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const fetchPromiseRef = useRef(null);

  const fetchData = async () => {
    if (!url) return;

    if (fetchPromiseRef.current) return fetchPromiseRef.current;

    setLoading(true);

    fetchPromiseRef.current = (async () => {
      try {
        setError(null);
        const res = await api.get(url);

        let normalized = [];
        if (res.data?.data) {
          normalized = Array.isArray(res.data.data) ? res.data.data : [res.data.data];
        } else if (Array.isArray(res.data)) {
          normalized = res.data;
        } else if (res.data && typeof res.data === "object") {
          normalized = [res.data];
        }

        setRows(normalized);
      } catch (err) {
        console.error("Error fetching data:", err);
        setRows([]);
        setError("Failed to fetch data.");
      } finally {
        fetchPromiseRef.current = null;
        setLoading(false);
      }
    })();

    return fetchPromiseRef.current;
  };

  useEffect(() => {
    fetchData();
  }, [url]);

  return (
    <div>
      <h1 className="text-xl font-bold mb-4">Data Table</h1>
      <p className="mb-2">
        Fetching from: <strong>{url}</strong>
      </p>

      <button
        onClick={fetchData}
        disabled={loading}
        className={`px-3 py-1 rounded mb-4 hover:bg-blue-600 ${
          loading ? "bg-gray-400 cursor-not-allowed" : "bg-blue-500 text-white"
        }`}
      >
        {loading ? "Loading..." : "Reload"}
      </button>

      {error && <p className="text-red-500 mb-2">{error}</p>}

      {rows.length > 0 ? (
        <table className="table-auto border-collapse border border-gray-300 w-full">
          <thead>
            <tr>
              {Object.keys(rows[0]).map((key) => (
                <th key={key} className="border border-gray-300 px-2 py-1 text-left">
                  {key}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) => (
              <tr key={i} className="odd:bg-white even:bg-gray-100">
                {Object.values(row).map((val, j) => (
                  <td key={j} className="border border-gray-300 px-2 py-1">
                    {renderValue(val)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <p>{loading ? "Loading data..." : "No data loaded yet."}</p>
      )}
    </div>
  );
}
