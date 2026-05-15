import { useState } from "react";
import { apiFetch } from "../services/apiFetch";
import { DIRECT_ENDPOINTS, QUERY_MAP } from "../utils/corqsreact";

const API_URL = "/api/corqs";

export default function CorqsTestPage() {
  const [expanded, setExpanded] = useState(null);
  const [args, setArgs] = useState({});
  const [results, setResults] = useState({});
  const [loading, setLoading] = useState(null);

  const toggleArgs = (key) =>
    setExpanded((prev) => (prev === key ? null : key));

  const setArg = (key, param, value) =>
    setArgs((prev) => ({ ...prev, [key]: { ...prev[key], [param]: value } }));

  const runCorqs = async (entry) => {
    setLoading(entry.resource);
    try {
      const params = args[entry.resource] ?? {};
      const result = await apiFetch(API_URL, { action: entry.action, params });
      setResults((prev) => ({ ...prev, [entry.resource]: result }));
    } catch (err) {
      setResults((prev) => ({ ...prev, [entry.resource]: { error: err.message } }));
    } finally {
      setLoading(null);
    }
  };

  const runDirect = (entry) => {
    const params = args[entry.name] ?? {};
    const qs = Object.entries(params)
      .filter(([, v]) => v !== "")
      .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
      .join("&");
    const url = qs ? `${entry.url}?${qs}` : entry.url;
    window.open(url, "_blank");
  };

  const btnClass = "px-3 py-1 text-sm rounded border";

  const renderArgsRow = (key, params) =>
    expanded === key && (
      <tr key={`${key}-args`}>
        <td colSpan={3} className="border border-gray-300 px-3 py-2 bg-yellow-50">
          <div className="flex flex-wrap gap-3">
            {params.map((param) => (
              <label key={param} className="flex items-center gap-2 text-sm">
                <span className="font-mono text-gray-600">{param}</span>
                <input
                  type="text"
                  value={args[key]?.[param] ?? ""}
                  onChange={(e) => setArg(key, param, e.target.value)}
                  className="border border-gray-300 rounded px-2 py-1 text-sm w-48"
                  placeholder={param}
                />
              </label>
            ))}
          </div>
        </td>
      </tr>
    );

  const tableHeader = (
    <thead>
      <tr className="bg-gray-100">
        <th className="border border-gray-300 px-3 py-2 text-left">API</th>
        <th className="border border-gray-300 px-3 py-2 text-left">Params</th>
        <th className="border border-gray-300 px-3 py-2 text-left w-32">Actions</th>
      </tr>
    </thead>
  );

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">CORQS API Test</h1>

      {/* CORQS APIs */}
      <h2 className="text-lg font-semibold mb-2">CORQS APIs</h2>
      <table className="w-full border-collapse border border-gray-300 text-sm mb-8">
        {tableHeader}
        <tbody>
          {QUERY_MAP.map((entry) => (
            <>
              <tr key={entry.resource} className="odd:bg-white even:bg-gray-50">
                <td className="border border-gray-300 px-3 py-2 font-mono">{entry.resource}</td>
                <td className="border border-gray-300 px-3 py-2 text-gray-500">
                  {entry.routeParams?.join(", ") ?? "—"}
                </td>
                <td className="border border-gray-300 px-3 py-2">
                  <div className="flex gap-2">
                    {entry.routeParams?.length > 0 && (
                      <button
                        onClick={() => toggleArgs(entry.resource)}
                        className={`${btnClass} border-gray-300 bg-gray-100 hover:bg-gray-200`}
                      >
                        Args
                      </button>
                    )}
                    <button
                      onClick={() => runCorqs(entry)}
                      disabled={loading === entry.resource}
                      className={`${btnClass} border-blue-400 bg-blue-500 text-white hover:bg-blue-600 disabled:opacity-50`}
                    >
                      {loading === entry.resource ? "..." : "Run"}
                    </button>
                  </div>
                </td>
              </tr>
              {renderArgsRow(entry.resource, entry.routeParams ?? [])}
              {results[entry.resource] !== undefined && (
                <tr key={`${entry.resource}-result`}>
                  <td colSpan={3} className="border border-gray-300 px-3 py-2 bg-green-50">
                    <pre className="text-xs overflow-auto max-h-48 whitespace-pre-wrap">
                      {JSON.stringify(results[entry.resource], null, 2)}
                    </pre>
                  </td>
                </tr>
              )}
            </>
          ))}
        </tbody>
      </table>

      {/* Direct GET Endpoints */}
      <h2 className="text-lg font-semibold mb-2">Direct Endpoints</h2>
      <table className="w-full border-collapse border border-gray-300 text-sm">
        {tableHeader}
        <tbody>
          {DIRECT_ENDPOINTS.map((entry) => (
            <>
              <tr key={entry.name} className="odd:bg-white even:bg-gray-50">
                <td className="border border-gray-300 px-3 py-2 font-mono">{entry.name}</td>
                <td className="border border-gray-300 px-3 py-2 text-gray-500">
                  {entry.params?.join(", ") ?? "—"}
                </td>
                <td className="border border-gray-300 px-3 py-2">
                  <div className="flex gap-2">
                    {entry.params?.length > 0 && (
                      <button
                        onClick={() => toggleArgs(entry.name)}
                        className={`${btnClass} border-gray-300 bg-gray-100 hover:bg-gray-200`}
                      >
                        Args
                      </button>
                    )}
                    <button
                      onClick={() => runDirect(entry)}
                      className={`${btnClass} border-green-400 bg-green-500 text-white hover:bg-green-600`}
                    >
                      Open
                    </button>
                  </div>
                </td>
              </tr>
              {renderArgsRow(entry.name, entry.params ?? [])}
            </>
          ))}
        </tbody>
      </table>
    </div>
  );
}
