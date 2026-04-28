import { useEffect, useState } from "react";
import api from "../services/http";
import { useAuth } from "./AuthContext";
import { GlobalContext } from "./GlobalStore";

export function GlobalProvider({ children }) {
  const { auth, logout } = useAuth();

  const [urlCalendar] = useState(
        "/api/Calendar/GetCalendar"
    // "https://localhost:44344/api/Calendar/GetCalendar"
  );
  // const [urlCases] = useState([
  //   "https://jsonplaceholder.typicode.com/albums"
  // ]);
  // const [urlTemplates] = useState("file://cases.json");
  // const [url] = useState("https://jsonplaceholder.typicode.com/users");


  //
  const [urlCases, setUrlCases] = useState(
  localStorage.getItem("urlCases") || "https://jsonplaceholder.typicode.com/albums"
);

const [urlTemplates, setUrlTemplates] = useState(
  localStorage.getItem("urlTemplates") || "file://cases.json"
);

const [url, setUrl] = useState(
  localStorage.getItem("url") || "https://jsonplaceholder.typicode.com/users"
);

  //

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

  useEffect(() => {
  localStorage.setItem("url", url);
}, [url]);

useEffect(() => {
  localStorage.setItem("urlCases", urlCases);
}, [urlCases]);

useEffect(() => {
  localStorage.setItem("urlTemplates", urlTemplates);
}, [urlTemplates]);


  return (
    <GlobalContext.Provider
    value={{
  urlCalendar,
  url,
  setUrl,
  urlCases,
  setUrlCases,
  urlTemplates,
  setUrlTemplates,
  loading,
  setLoading
}}

      // value={{
      //   urlCalendar,
      //   urlCases,
      //   urlTemplates,
      //   url,
      //   loading,
      //   setLoading
      // }}
    >
      {children}
    </GlobalContext.Provider>
  );
}
