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

  // CORQS / wrapped: { data: [...] or {...} }
  if (data?.data !== undefined) {
    if (Array.isArray(data.data)) return data.data;
    if (data.data !== null && typeof data.data === "object") return [data.data];
    return [];
  }

  // Raw array
  if (Array.isArray(data)) return data;

  // Single object
  if (typeof data === "object") return [data];

  return [];
};
