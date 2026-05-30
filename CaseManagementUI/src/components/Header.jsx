import { useEffect, useRef, useState } from "react";
import { useAuth } from "../context/AuthContext";

const DUMMY_PROFILE = {
  name:  "Frank Mastronardi",
  email: "frank@casemanagement.local",
  role:  "Administrator",
};

function getDisplayName(user) {
  if (!user) return DUMMY_PROFILE.name;
  return user.username ?? user.name ?? user.email ?? DUMMY_PROFILE.name;
}

function getInitials(displayName) {
  const parts = displayName.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  return displayName.slice(0, 2).toUpperCase();
}

export default function Header() {
  const { auth, logout } = useAuth();
  const [open, setOpen]  = useState(false);
  const ref              = useRef(null);

  const displayName = getDisplayName(auth.user);
  const initials    = getInitials(displayName);
  const email       = auth.user?.email ?? DUMMY_PROFILE.email;
  const role        = auth.user?.role  ?? DUMMY_PROFILE.role;

  useEffect(() => {
    const handler = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  return (
    <header className="h-8 bg-gray-800 text-gray-400 text-xs flex items-center justify-between px-4 flex-shrink-0 border-b border-gray-700 relative z-50">
      <span className="text-gray-300 font-medium tracking-wide">Case Management</span>

      <div ref={ref} className="relative">
        <button
          onClick={() => setOpen((o) => !o)}
          className="flex items-center gap-2 hover:text-gray-200 transition-colors cursor-pointer"
        >
          <span>{displayName}</span>
          <div className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center font-semibold text-[10px]">
            {initials}
          </div>
        </button>

        {open && (
          <div className="absolute right-0 top-full mt-1 w-52 bg-white border border-gray-200 rounded shadow-lg text-gray-700">
            <div className="px-4 py-3 border-b border-gray-100">
              <div className="font-semibold text-sm text-gray-900">{displayName}</div>
              <div className="text-xs text-gray-500 mt-0.5">{email}</div>
              <div className="text-xs text-blue-600 mt-0.5">{role}</div>
            </div>
            <div className="py-1">
              <button
                onClick={logout}
                className="w-full text-left px-4 py-2 text-xs hover:bg-gray-50 text-red-600"
              >
                Sign out
              </button>
            </div>
          </div>
        )}
      </div>
    </header>
  );
}
