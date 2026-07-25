import { Link } from "react-router-dom";

// Reusable page-level header: title (one consistent style everywhere), an optional
// breadcrumb trail back through parent pages, and an optional right-aligned actions slot
// (same button-list shape as XyzTablePage's tableActions).
export default function PageHeader({ title, breadcrumbs = [], actions = [] }) {
  return (
    <div className="mb-4">
      {breadcrumbs.length > 0 && (
        <nav className="text-sm text-gray-500 mb-1 flex items-center gap-1">
          {breadcrumbs.map((crumb, i) => (
            <span key={i} className="flex items-center gap-1">
              {i > 0 && <span className="text-gray-300">/</span>}
              {crumb.to ? (
                <Link to={crumb.to} className="hover:text-blue-600 hover:underline">
                  {crumb.label}
                </Link>
              ) : (
                <span className="text-gray-700 font-medium">{crumb.label}</span>
              )}
            </span>
          ))}
        </nav>
      )}

      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-gray-900">{title}</h1>

        {actions.length > 0 && (
          <div className="flex items-center gap-2">
            {actions.map((action, i) => (
              <button
                key={i}
                onClick={action.onClick}
                disabled={action.disabled}
                className={
                  action.className ||
                  "px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed"
                }
              >
                {action.label}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
