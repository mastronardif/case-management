import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { useGlobalStore } from "../context/GlobalStore";

export default function Settings() {
  const { auth } = useAuth(); // ✅ use AuthContext
  const { url, setUrl, urlCases, setUrlCases, urlTemplates, setUrlTemplates} = useGlobalStore();
  const [inputValue, setInputValue] = useState(url);
  const [inputCases, setInputCases] = useState(urlCases);  
  const [inputTemplates, setInputTemplates] = useState(urlTemplates);

  const handleSave = () => {
    setUrl(inputValue);
    setUrlCases(inputCases);
    setUrlTemplates(inputTemplates);
  }

  return (
    <div>
      <h1 className="text-xl font-bold mb-4">Settings</h1>

      {/* API URL */}
      <div className="mb-6">
        <label className="block mb-2 font-medium" htmlFor="url">API URL</label>
        <input
          id="url"
          type="text"
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2"
        />

      {/* Cases URL */}
      <div className="mb-4">
        <label className="block mb-2 font-medium">Cases API URL</label>
        <input
          type="text"
          value={inputCases}
          onChange={(e) => setInputCases(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2"
        />
        </div>

        <div className="mb-4">
        <label className="block mb-2 font-medium">Templates URL</label>
        <input
          type="text"
          value={inputTemplates}
          onChange={(e) => setInputTemplates(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2"
        />
        </div>

        <button onClick={handleSave} className="mt-2 px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600">
          Save
        </button>
        <p className="mt-2 text-gray-600 text-sm">
          Changing this URL will automatically refresh the table data.
        </p>
      </div>

      {/* Authentication Info */}
      <div className="p-4 border rounded bg-gray-50">
        <h2 className="text-lg font-semibold mb-2">Authentication</h2>
        {auth?.token ? (
          <>
            <p><strong>User:</strong> {auth.user}</p>
            <p><strong>Roles:</strong> {auth.roles.join(", ")}</p>
            <p className="break-all"><strong>Token:</strong> {auth.token}</p>
          </>
        ) : (
          <p className="text-gray-600">Not logged in</p>
        )}
      </div>
    </div>
  );
}
