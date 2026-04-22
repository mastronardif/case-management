import { useState } from "react";

export default function TableActions({
  title,
  onReload,
  onNew,
  onExport,
  onSearch,
  loading = false,
}) {
  const [searchInput, setSearchInput] = useState("");

  const handleSearchChange = (e) => {
    const value = e.target.value;
    setSearchInput(value);
    onSearch(value);
  };

  return (
    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-4">
      <h2 className="text-xl font-bold">{title}</h2>

      <div className="flex flex-wrap gap-2">
        <input
          type="text"
          value={searchInput}
          onChange={handleSearchChange}
          placeholder="Search..."
          className="px-3 py-1 border rounded shadow-sm focus:outline-none focus:ring focus:border-blue-300"
        />

        <button
          onClick={onReload}
          disabled={loading}
          className={`px-3 py-1 rounded ${
            loading ? "bg-gray-400 cursor-not-allowed" : "bg-blue-500 text-white hover:bg-blue-600"
          }`}
        >
          {loading ? "Loading..." : "Reload"}
        </button>

        <button
          onClick={onNew}
          className="px-3 py-1 rounded bg-green-500 text-white hover:bg-green-600"
        >
          New
        </button>

        <button
          onClick={onExport}
          className="px-3 py-1 rounded bg-yellow-500 text-white hover:bg-yellow-600"
        >
          Export
        </button>
      </div>
    </div>
  );
}
