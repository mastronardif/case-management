// src/components/ActionPage.jsx

export default function ActionPage({ actions = [], buttonClass }) {
  if (!actions.length) return null;

  return (
    <div className="flex items-center gap-2">
      {actions.map((action, i) => (
        <button
          key={i}
          onClick={() => action.onClick?.()}
          className={`${buttonClass} ${action.className ?? ""}`}
        >
        {action.label}
        </button>
      ))}
    </div>
  );
}
