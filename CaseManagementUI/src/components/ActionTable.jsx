import { useState } from "react";

export default function ActionTable({
  title,
  count,
  onNew,
  onReload,
  onExport,
  onSearch,
  loading = false,
  showNew = true,
  showReload = true,
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
      {title && (
        <span className="text-lg font-semibold mr-2">
          {title}
          {typeof count === "number" && (
            <span className="text-gray-400 font-normal"> ({count})</span>
          )}
        </span>
      )}
      {showNew && (
        <button onClick={onNew} className={buttonClass} disabled={loading}>
          New
        </button>
      )}
      {showReload && (
        <button onClick={onReload} className={buttonClass} disabled={loading}>
          Reload
        </button>
      )}
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
