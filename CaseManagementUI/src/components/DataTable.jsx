import { saveAs } from "file-saver";
import React, { forwardRef, useImperativeHandle } from "react";

const DataTable = forwardRef(({ rows }, ref) => {

  // 👇 ALWAYS define exportCSV, even if rows is empty
  const exportCSV = () => {
    if (!rows || rows.length === 0) return;

    const columns = Array.from(
      rows.reduce((set, row) => {
        Object.keys(row).forEach((key) => set.add(key));
        return set;
      }, new Set())
    );

    const safeStringify = (obj) => {
      const seen = new WeakSet();
      return JSON.stringify(obj, (key, value) => {
        if (typeof value === "object" && value !== null) {
          if (seen.has(value)) return "[Circular]";
          seen.add(value);
        }
        return value;
      });
    };

    const csvRows = [];
    csvRows.push(columns.join(","));

    rows.forEach((row) => {
      const values = columns.map((col) => {
        let val = row[col];
        if (val === null || val === undefined) val = "";
        else if (typeof val === "object") val = safeStringify(val);

        if (typeof val === "string" && (val.includes(",") || val.includes('"'))) {
          val = `"${val.replace(/"/g, '""')}"`;
        }
        return val;
      });
      csvRows.push(values.join(","));
    });

    const blob = new Blob([csvRows.join("\n")], {
      type: "text/csv;charset=utf-8;"
    });
    saveAs(blob, "export.csv");
  };

  // ✅ Hook is now ALWAYS called
  useImperativeHandle(ref, () => ({
    exportCSV,
  }));

  // 👇 Early return AFTER hooks
  if (!rows || rows.length === 0) return null;

  const columns = Array.from(
    rows.reduce((set, row) => {
      Object.keys(row).forEach((key) => set.add(key));
      return set;
    }, new Set())
  );

  const renderValue = (val) => {
    if (val === null || val === undefined) return "";
    if (React.isValidElement(val)) return val;
    if (
      typeof val === "string" ||
      typeof val === "number" ||
      typeof val === "boolean"
    )
      return val;
    try {
      return JSON.stringify(val);
    } catch {
      return "[object]";
    }
  };

  return (
    <div>
      <table className="w-full border-collapse border border-gray-300">
        <thead>
          <tr>
            {columns.map((col) => (
              <th
                key={col}
                className="border border-gray-300 px-2 py-1 text-left bg-gray-100"
              >
                {col}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, idx) => (
            <tr
              key={idx}
              className={idx % 2 === 0 ? "bg-white" : "bg-gray-50"}
            >
              {columns.map((col) => (
                <td key={col} className="border border-gray-300 px-2 py-1">
                  {renderValue(row[col])}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
});

export default DataTable;
