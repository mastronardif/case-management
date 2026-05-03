import api from "./http";

/**
 * Flexible API helper for GET or POST (CORQS + REST friendly)
 *
 * usage:
 * apiFetch("/api/users")
 * apiFetch("/api/corqs", { action: "...", params: {...} })
 * apiFetch("/api/users", null, { headers... })
 */
export const apiFetch = async (url, body = null, config = {}) => {
  try {
    let res;

    // POST if body exists, otherwise GET
    if (body) {
      res = await api.post(url, body, config);
    } else {
      res = await api.get(url, config);
    }

    return normalizeResponse(res);
  } catch (err) {
    console.error("apiFetch error:", err);
    throw err;
  }
};

/**
 * Normalizes common API response shapes into a table-friendly array
 */
const normalizeResponse = (res) => {
  const data = res?.data;

  if (!data) return [];

  // common CORQS shape: { data: [...] }
  if (data?.data) {
    return Array.isArray(data.data) ? data.data : [data.data];
  }

  // raw array
  if (Array.isArray(data)) return data;

  // single object
  if (typeof data === "object") return [data];

  return [];
};
