import { NavLink } from "react-router-dom";
import routes from "../routes.jsx";

export default function Sidebar({ open, toggle }) {
  const getRouteLink = (route) => {
    if (route.link) return route.link;
    if (route.defaultParams)
      return route.path.replace(":vvv?", route.defaultParams.vvv);
    return route.path.replace(":vvv?", "");
  };

  return (
    <aside
      className={`bg-gray-800 text-white transition-all duration-300 flex flex-col h-full ${
        open ? "w-44" : "w-12"
      }`}
    >
      <button
        onClick={toggle}
        className="p-3 hover:bg-gray-700 text-left text-xl leading-none flex-shrink-0"
        title={open ? "Collapse" : "Expand"}
      >
        ☰
      </button>

      {open && (
        <div className="px-4 pb-3 font-bold text-lg border-b border-gray-700">
          Dyno Minds ©
        </div>
      )}

      <nav className="flex flex-col mt-1 flex-1 overflow-y-auto">
        {routes
          .filter((r) => !r.hideFromNav)
          .map((route) => (
            <NavLink
              key={route.path}
              to={getRouteLink(route)}
              end
              title={!open ? (route.label || route.name) : undefined}
              className={({ isActive }) =>
                `block px-3 py-2 rounded hover:bg-gray-700 truncate ${
                  isActive ? "bg-gray-900" : ""
                }`
              }
            >
              {open ? (route.label || route.name) : "·"}
            </NavLink>
          ))}
      </nav>
    </aside>
  );
}
