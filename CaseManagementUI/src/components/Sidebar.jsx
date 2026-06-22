import { useState } from "react";
import { NavLink } from "react-router-dom";
import routes from "../routes.jsx";

export default function Sidebar({ open, toggle }) {
  const getRouteLink = (route) => {
    if (route.link) return route.link;
    if (route.defaultParams)
      return route.path.replace(":vvv?", route.defaultParams.vvv);
    return route.path.replace(":vvv?", "");
  };

  const navRoutes = routes.filter((route) => !route.hideFromNav);
  const groups = navRoutes.reduce((acc, route) => {
    const key = route.navGroup || "Main";
    if (!acc[key]) acc[key] = [];
    acc[key].push(route);
    return acc;
  }, {});

  const [expandedGroups, setExpandedGroups] = useState({
    Experimental: true,
  });

  const toggleGroup = (groupName) => {
    setExpandedGroups((prev) => ({
      ...prev,
      [groupName]: !prev[groupName],
    }));
  };

  return (
    <>
      {open && (
        <div
          className="fixed inset-0 bg-black/40 z-30 sm:hidden"
          onClick={toggle}
        />
      )}
      <aside
        className={`bg-gray-800 text-white transition-all duration-300 flex flex-col h-full
          fixed inset-y-0 left-0 z-40
          sm:relative sm:z-auto sm:translate-x-0
          ${open ? "w-44 translate-x-0" : "-translate-x-full sm:translate-x-0 sm:w-12"}`}
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
          {Object.entries(groups).map(([groupName, groupRoutes]) => (
            <div key={groupName}>
              {open && groupName !== "Main" && (
                <button
                  type="button"
                  onClick={() => toggleGroup(groupName)}
                  className="flex w-full items-center justify-between px-3 pt-3 pb-1 text-[10px] uppercase tracking-wide text-gray-400 hover:text-white"
                >
                  <span>{groupName}</span>
                  <span>{expandedGroups[groupName] ? "▾" : "▸"}</span>
                </button>
              )}
              {(!open || expandedGroups[groupName] !== false) &&
                groupRoutes.map((route) => (
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
            </div>
          ))}
        </nav>
      </aside>
    </>
  );
}
