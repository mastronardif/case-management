import { useEffect, useState } from "react";
import api from "../services/http";
import { useAuth } from "./AuthContext";
import { GlobalContext } from "./GlobalStore";

export const DEFAULTS = {
  urlCases:     "/api/corqs",
  urlTemplates: "/files/index.json",
  url:          "/api/corqs",
};

// These URLs are free-text editable on the (Experimental) Settings page and persisted per-device
// in localStorage. A value typed/pasted with a scheme+host baked in (e.g. "https://localhost:44344/
// api/corqs") "works" on whichever device saved it, since that device's own localhost means itself
// — then silently breaks on every other device loading the same saved value, since localhost means
// something different there. Stripping to just path+search makes that failure mode impossible: an
// already-relative value passes through unchanged, an absolute one degrades to its path.
function toRelativePath(value) {
  if (!value) return value;
  try {
    const u = new URL(value, window.location.origin);
    return u.pathname + u.search;
  } catch {
    return value;
  }
}

export function GlobalProvider({ children }) {
  const { auth, logout } = useAuth();

  const [urlCases,     setUrlCasesRaw]     = useState(toRelativePath(localStorage.getItem("urlCases")     || DEFAULTS.urlCases));
  const [urlTemplates, setUrlTemplatesRaw] = useState(toRelativePath(localStorage.getItem("urlTemplates") || DEFAULTS.urlTemplates));
  const [url,          setUrlRaw]          = useState(toRelativePath(localStorage.getItem("url")          || DEFAULTS.url));

  const setUrlCases     = (v) => setUrlCasesRaw(toRelativePath(v));
  const setUrlTemplates = (v) => setUrlTemplatesRaw(toRelativePath(v));
  const setUrl          = (v) => setUrlRaw(toRelativePath(v));

  const [body, setBody] = useState(() => {
    const saved = localStorage.getItem("body");
    return saved ? JSON.parse(saved) : null;
  });

  const [loading, setLoading] = useState(false);
  const [requestCount, setRequestCount] = useState(0);
  const [tenantId, setTenantId] = useState(null);

  useEffect(() => {
    const reqInterceptor = api.interceptors.request.use((config) => {
      if (auth.token) config.headers.Authorization = `Bearer ${auth.token}`;
      setRequestCount((c) => c + 1);
      setLoading(true);
      return config;
    });

    const resInterceptor = api.interceptors.response.use(
      (res) => {
        setRequestCount((c) => c - 1);
        const tenant = res.headers["tenant-id"];
        if (tenant) setTenantId(tenant);
        return res;
      },
      (err) => {
        setRequestCount((c) => c - 1);
        if (err.response?.status === 401) logout();
        return Promise.reject(err);
      }
    );

    return () => {
      api.interceptors.request.eject(reqInterceptor);
      api.interceptors.response.eject(resInterceptor);
    };
  }, [auth.token, logout]);

  useEffect(() => { setLoading(requestCount > 0); }, [requestCount]);

  useEffect(() => localStorage.setItem("url",          url),          [url]);
  useEffect(() => localStorage.setItem("urlCases",     urlCases),     [urlCases]);
  useEffect(() => localStorage.setItem("urlTemplates", urlTemplates), [urlTemplates]);
  useEffect(() => { localStorage.setItem("body", JSON.stringify(body)); }, [body]);

  return (
    <GlobalContext.Provider
      value={{
        url, setUrl,
        body, setBody,
        urlCases, setUrlCases,
        urlTemplates, setUrlTemplates,
        loading, setLoading,
        tenantId,
      }}
    >
      {children}
    </GlobalContext.Provider>
  );
}
