import { saveAs } from "file-saver";
import React, { forwardRef, useImperativeHandle, useMemo } from "react";

const escapeCsvValue = (value) => {
  if (value === null || value === undefined) return "";
  if (typeof value === "object") {
    try {
      return JSON.stringify(value);
    } catch {
      return "[object]";
    }
  }
  return String(value);
};

const DataTable = forwardRef(
  (
    {
      rows = [],
      columns,
      actions = [],
      emptyMessage = "No data available.",
      className = "",
      tableClassName = "w-full border-collapse border border-gray-300",
      headerClassName = "border border-gray-300 px-2 py-1 text-left bg-gray-100",
      cellClassName = "border border-gray-300 px-2 py-1",
      rowClassName = (index) => (index % 2 === 0 ? "bg-white" : "bg-gray-50"),
      getRowKey = (_, index) => index,
    },
    ref
  ) => {
    const resolvedColumns = useMemo(() => {
      if (Array.isArray(columns) && columns.length > 0) {
        return columns;
      }

      const collected = new Set();
      rows.forEach((row) => {
        Object.keys(row || {}).forEach((key) => collected.add(key));
      });
      return Array.from(collected);
    }, [columns, rows]);

    const exportCSV = () => {
      if (!rows || rows.length === 0) return;

      const csvRows = [];
      csvRows.push(resolvedColumns.join(","));

      rows.forEach((row) => {
        const values = resolvedColumns.map((col) => {
          const rawValue = row?.[col];
          const value = escapeCsvValue(rawValue);
          const needsQuotes = /[",\n]/.test(value);
          return needsQuotes ? `"${value.replace(/"/g, '""')}"` : value;
        });
        csvRows.push(values.join(","));
      });

      const blob = new Blob([csvRows.join("\n")], {
        type: "text/csv;charset=utf-8;",
      });
      saveAs(blob, "export.csv");
    };

    useImperativeHandle(
      ref,
      () => ({
        exportCSV,
      }),
      [exportCSV]
    );

    const renderValue = (value) => {
      if (value === null || value === undefined) return "";
      if (React.isValidElement(value)) return value;
      if (typeof value === "object") {
        try {
          return JSON.stringify(value);
        } catch {
          return "[object]";
        }
      }
      return String(value);
    };

    if (!rows || rows.length === 0) {
      return <div className={className}>{emptyMessage}</div>;
    }

    return (
      <div className={className}>
        <div className="w-full overflow-x-auto">
          <table className={tableClassName}>
            <thead>
              <tr>
                {resolvedColumns.map((col) => (
                  <th key={col} className={headerClassName}>
                    {col}
                  </th>
                ))}
                {actions.length > 0 && (
                  <th key="__actions__" className={headerClassName}>
                    Actions
                  </th>
                )}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, index) => (
                <tr key={getRowKey(row, index)} className={rowClassName(index)}>
                  {resolvedColumns.map((col) => (
                    <td key={col} className={cellClassName}>
                      {renderValue(row?.[col])}
                    </td>
                  ))}
                  {actions.length > 0 && (
                    <td key="__actions__" className={cellClassName}>
                      <div className="flex flex-wrap gap-1">
                        {actions.map((action, actionIndex) => (
                          <button
                            key={`${action.label || "action"}-${actionIndex}`}
                            type="button"
                            onClick={() => action.onClick?.(row)}
                            className={action.className || "px-2 py-1 text-xs bg-green-500 text-white rounded hover:bg-green-600"}
                            disabled={action.disabled}
                          >
                            {action.label}
                          </button>
                        ))}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    );
  }
);

DataTable.displayName = "DataTable";

export default DataTable;
