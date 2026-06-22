import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import ActionTable from "../components/ActionTable";
import DataTable from "../components/DataTable";
import api from "../services/http";

const isSearchableValue = (value) => {
  if (value === null || value === undefined) return false;
  if (typeof value === "object" && value?.$$typeof) return false;
  return true;
};

export default function XyzTablePage({
  title,
  fetchUrl,
  rows: rowsProp,
  ActionRowComponent,
  rowActions = [],
  tableActions = [],
  emptyMessage,
  newPath,
}) {
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const navigate = useNavigate();
  const dataTableRef = useRef(null);

  const fetchData = useCallback(async () => {
    if (!fetchUrl) return;
    setLoading(true);
    setError(null);
    try {
      const res = await api.get(fetchUrl);
      const json = res.data;

      let normalized = [];
      if (json?.data) normalized = Array.isArray(json.data) ? json.data : [json.data];
      else if (Array.isArray(json)) normalized = json;
      else if (json && typeof json === "object") normalized = [json];

      setRows(normalized);
    } catch (err) {
      console.error(`Error fetching ${title}:`, err);
      setRows([]);
      setError(`Failed to fetch ${title.toLowerCase()}.`);
    } finally {
      setLoading(false);
    }
  }, [fetchUrl, title]);

  useEffect(() => {
    if (rowsProp !== undefined) {
      setRows(rowsProp);
    } else if (fetchUrl) {
      fetchData();
    }
  }, [rowsProp, fetchUrl, fetchData]);

  const handleExport = () => {
    dataTableRef.current?.exportCSV?.();
  };

  const handleNew = () => {
    if (newPath) {
      navigate(newPath);
      return;
    }

    navigate(`/${title.toLowerCase().replace(/\s+/g, "-")}/new`);
  };

  const effectiveRows = useMemo(
    () => (rowsProp !== undefined ? rowsProp : rows),
    [rowsProp, rows]
  );

  const filteredRows = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    if (!normalizedSearch) return effectiveRows;

    return effectiveRows.filter((row) =>
      Object.values(row ?? {}).some((value) => {
        if (!isSearchableValue(value)) return false;
        if (typeof value === "object") {
          return JSON.stringify(value).toLowerCase().includes(normalizedSearch);
        }
        return String(value).toLowerCase().includes(normalizedSearch);
      })
    );
  }, [effectiveRows, search]);

  const greyButtonClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed";

  return (
    <div className="relative p-4 sm:p-6 flex justify-center">
      <div className="relative w-full max-w-6xl rounded shadow bg-white border border-gray-200 flex flex-col">
        <div className="flex items-center justify-between bg-gray-50 border-b border-gray-200 px-4 py-2 flex-shrink-0">
          <h2 className="font-semibold text-lg">{title}</h2>
          <div className="flex items-center gap-2">
            <ActionTable
              title={title}
              onReload={fetchUrl ? fetchData : undefined}
              loading={loading}
              onNew={handleNew}
              onExport={handleExport}
              onSearch={setSearch}
              buttonClass={greyButtonClass}
            />
            {tableActions.map((action, i) => (
              <button
                key={i}
                onClick={() => action.onClick?.()}
                className={greyButtonClass}
              >
                {action.label}
              </button>
            ))}
          </div>
        </div>

        {error && <p className="text-red-500 px-4 py-2">{error}</p>}

        <div className="overflow-x-auto flex-1">
          {filteredRows.length > 0 ? (
            <DataTable
              ref={dataTableRef}
              rows={filteredRows.map((row) => {
                if ("Action" in row) return row;
                return {
                  ...row,
                  Action:
                    ActionRowComponent && rowActions.length > 0 ? (
                      <ActionRowComponent row={row} actions={rowActions} />
                    ) : null,
                };
              })}
            />
          ) : (
            <p className="mt-2 text-gray-500 px-4 py-2">
              {loading ? `Loading ${title}...` : emptyMessage || `No ${title} found.`}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
