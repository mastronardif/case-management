// Column order for a table, left to right by importance. `preferred` is the `columns` list an API
// declares in schema.json. Anything it doesn't mention still shows, after the listed ones, so a new
// column in the SQL never silently disappears. No list -> undefined, i.e. DataTable's own default
// (row key order).
export function orderColumns(rows, preferred) {
  if (!Array.isArray(preferred) || preferred.length === 0) return undefined;

  const present = new Set();
  rows.forEach((row) => Object.keys(row ?? {}).forEach((key) => present.add(key)));

  return [
    ...preferred.filter((column) => present.has(column)),
    ...[...present].filter((column) => !preferred.includes(column)),
  ];
}
