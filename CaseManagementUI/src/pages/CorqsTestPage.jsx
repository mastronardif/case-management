import { useState } from "react";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";

const API_URL = "/api/corqs";

export default function CorqsTestPage() {
  const [expanded, setExpanded] = useState(null);
  const [args, setArgs] = useState({});
  const [results, setResults] = useState({});
  const [loading, setLoading] = useState(null);

  const toggleArgs = (resource) =>
    setExpanded((prev) => (prev === resource ? null : resource));

  const setArg = (resource, key, value) =>
    setArgs((prev) => ({
      ...prev,
      [resource]: { ...prev[resource], [key]: value },
    }));

  const run = async (entry) => {
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

  const btnClass = "px-3 py-1 text-sm rounded border";

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">CORQS API Test</h1>

      <table className="w-full border-collapse border border-gray-300 text-sm">
        <thead>
          <tr className="bg-gray-100">
            <th className="border border-gray-300 px-3 py-2 text-left">API</th>
            <th className="border border-gray-300 px-3 py-2 text-left">Params</th>
            <th className="border border-gray-300 px-3 py-2 text-left w-32">Actions</th>
          </tr>
        </thead>
        <tbody>
          {QUERY_MAP.map((entry) => (
            <>
              <tr key={entry.resource} className="odd:bg-white even:bg-gray-50">
                <td className="border border-gray-300 px-3 py-2 font-mono">
                  {entry.resource}
                </td>
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
                      onClick={() => run(entry)}
                      disabled={loading === entry.resource}
                      className={`${btnClass} border-blue-400 bg-blue-500 text-white hover:bg-blue-600 disabled:opacity-50`}
                    >
                      {loading === entry.resource ? "..." : "Run"}
                    </button>
                  </div>
                </td>
              </tr>

              {expanded === entry.resource && (
                <tr key={`${entry.resource}-args`}>
                  <td colSpan={3} className="border border-gray-300 px-3 py-2 bg-yellow-50">
                    <div className="flex flex-wrap gap-3">
                      {entry.routeParams.map((param) => (
                        <label key={param} className="flex items-center gap-2 text-sm">
                          <span className="font-mono text-gray-600">{param}</span>
                          <input
                            type="text"
                            value={args[entry.resource]?.[param] ?? ""}
                            onChange={(e) => setArg(entry.resource, param, e.target.value)}
                            className="border border-gray-300 rounded px-2 py-1 text-sm w-48"
                            placeholder={param}
                          />
                        </label>
                      ))}
                    </div>
                  </td>
                </tr>
              )}

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
    </div>
  );
}
