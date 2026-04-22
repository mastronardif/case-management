import { createContext, useContext, useEffect, useState } from "react";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState({ token: null, user: null, roles: [] });

  const login = (token, user, roles = []) => {
    setAuth({ token, user, roles });
    localStorage.setItem("token", token);
    localStorage.setItem("user", JSON.stringify(user));
    localStorage.setItem("roles", JSON.stringify(roles));
  };

  const logout = () => {
    setAuth({ token: null, user: null, roles: [] });
    localStorage.clear();
  };

  // Restore auth from localStorage on mount
  useEffect(() => {
    const token = localStorage.getItem("token");
    const user = localStorage.getItem("user");
    const roles = localStorage.getItem("roles");
    if (token && user) {
      setAuth({ token, user: JSON.parse(user), roles: JSON.parse(roles) || [] });
    }
  }, []);

  return (
    <AuthContext.Provider value={{ auth, login, logout, isAuthenticated: !!auth.token }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
