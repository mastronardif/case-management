// src/components/ActionRow.jsx
export default function ActionRow({ row, actions = [] }) {
  return (
    <div className="flex gap-2">
      {actions.map(({ label, onClick, className }, idx) => (
        <button
          key={idx}
          onClick={() => onClick(row)}
          className={`px-2 py-1 text-sm rounded transition ${className}`}
        >
          {label}
        </button>
      ))}
    </div>
  );
}
