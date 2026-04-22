import { NavLink } from "react-router-dom";
import routes from "../routes.jsx";

export default function Sidebar({ open, toggle }) {
  const getRouteLink = (route) => {
    if (route.defaultParams) {
      return route.path.replace(":vvv?", route.defaultParams.vvv);
    }
    return route.path.replace(":vvv?", "");
  };

  return (
    <aside
      className={`bg-gray-800 text-white transition-all duration-300 ${
        open ? "w-64" : "w-16"
      }`}
    >
      <div className="p-4 font-bold text-lg">
        Dyno Minds ©
      </div>

      <nav className="flex flex-col">
        {routes
          .filter((r) => !r.hideFromNav)
          .map((route) => (
            <NavLink
              key={route.path}
              to={getRouteLink(route)}
              end
              className={({ isActive }) =>
                `block px-4 py-2 rounded hover:bg-gray-700 ${
                  isActive ? "bg-gray-900" : ""
                }`
              }
            >
              {route.label || route.name}
            </NavLink>
          ))}
      </nav>

      <button
        onClick={toggle}
        className="mt-auto p-2 bg-gray-700 hover:bg-gray-600 w-full text-sm"
      >
        {open ? "Collapse" : "Expand"}
      </button>
    </aside>
  );
}
