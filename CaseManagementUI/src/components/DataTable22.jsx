const renderValue = (val) => {
  if (val === null || val === undefined) return "";

  // If it's already a React element (like your Action buttons), render it directly
  if (typeof val === "object" && val.$$typeof) {
    return val;
  }

  if (typeof val === "object") {
    return Object.entries(val)
      .map(([k, v]) => `${k}: ${renderValue(v)}`)
      .join(", ");
  }

  return val.toString();
};

export default function DataTable22({ rows = [] }) {
  if (!rows.length) {
    return <p>No data available.</p>;
  }

  const columns = Object.keys(rows[0]);

  return (
    <table className="table-auto border-collapse border border-gray-300 w-full">
      <thead>
        <tr className="bg-gray-100">
          {columns.map((col) => (
            <th
              key={col}
              className="border border-gray-300 px-2 py-1 text-left"
            >
              {col}
            </th>
          ))}
        </tr>
      </thead>

      <tbody>
        {rows.map((row, i) => (
          <tr key={i} className="odd:bg-white even:bg-gray-100">
            {columns.map((col) => (
              <td
                key={col}
                className="border border-gray-300 px-2 py-1"
              >
                {renderValue(row[col])}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
