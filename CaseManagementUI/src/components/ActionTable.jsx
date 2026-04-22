import { useState } from "react";

export default function ActionTable({
  title,
  onNew,
  onReload,
  onExport,
  onSearch,
  loading = false,
  buttonClass, // receives grey button style from parent
}) {
  const [searchValue, setSearchValue] = useState("");

  const handleSearchChange = (e) => {
    const val = e.target.value;
    setSearchValue(val);
    onSearch?.(val);
  };

  return (
    <div className="flex items-center gap-2">
      <button onClick={onNew} className={buttonClass} disabled={loading}>
        New
      </button>
      <button onClick={onReload} className={buttonClass} disabled={loading}>
        Reload
      </button>
      <button onClick={onExport} className={buttonClass} disabled={loading}>
        Export
      </button>
      <input
        type="text"
        placeholder="Search..."
        value={searchValue}
        onChange={handleSearchChange}
        className="h-9 px-3 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-1 focus:ring-blue-400 flex-shrink-0"
      />
    </div>
  );
}
