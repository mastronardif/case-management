import { useEffect, useState } from "react";
import api from "../services/http";
import { useAuth } from "./AuthContext";
import { GlobalContext } from "./GlobalStore";

export function GlobalProvider({ children }) {
  const { auth, logout } = useAuth();

  const [urlCalendar] = useState("/api/Calendar/GetCalendar");

  const [urlCases, setUrlCases] = useState(
    localStorage.getItem("urlCases") || "https://jsonplaceholder.typicode.com/albums"
  );

  const [urlTemplates, setUrlTemplates] = useState(
    localStorage.getItem("urlTemplates") || "file://cases.json"
  );

  const [url, setUrl] = useState(
    localStorage.getItem("url") || "https://jsonplaceholder.typicode.com/users"
  );
  
  const [body, setBody] = useState(() => {
      const saved = localStorage.getItem("body");
      return saved ? JSON.parse(saved) : null;
    });

  const [loading, setLoading] = useState(false);
  const [requestCount, setRequestCount] = useState(0);

  useEffect(() => {
    const reqInterceptor = api.interceptors.request.use((config) => {
      if (auth.token) {
        config.headers.Authorization = `Bearer ${auth.token}`;
      }
      setRequestCount((c) => c + 1);
      setLoading(true);
      return config;
    });

    const resInterceptor = api.interceptors.response.use(
      (res) => {
        setRequestCount((c) => c - 1);
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

  useEffect(() => {
    setLoading(requestCount > 0);
  }, [requestCount]);

  useEffect(() => localStorage.setItem("url", url), [url]);
  useEffect(() => localStorage.setItem("urlCases", urlCases), [urlCases]);
  useEffect(() => localStorage.setItem("urlTemplates", urlTemplates), [urlTemplates]);
  useEffect(() => {  localStorage.setItem("body", JSON.stringify(body));}, [body]);


  return (
    <GlobalContext.Provider
      value={{
        urlCalendar,
        url,
        setUrl,
        body,
        setBody,
        urlCases,
        setUrlCases,
        urlTemplates,
        setUrlTemplates,
        loading,
        setLoading,
      }}
    >
      {children}
    </GlobalContext.Provider>
  );
}
