// Generic per-row action-button strip for XyzTablePage's rowActions/ActionRowComponent slot —
// same shape FilesTablePage's local FileActionRow already used; shared here instead of
// re-copied per page.
export default function ActionRow({ row, actions }) {
  return (
    <div className="flex gap-1">
      {actions.map((action, i) => (
        <button
          key={action.label ?? i}
          onClick={() => action.onClick(row)}
          className={action.className || "px-3 py-1 text-sm rounded bg-green-500 text-white hover:bg-green-600"}
        >
          {action.label}
        </button>
      ))}
    </div>
  );
}
