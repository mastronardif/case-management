import { QUERY_MAP } from "./corqsreact.js";

export function buildQuery(resource, type, id, state = {}) {
  const entry = QUERY_MAP.find(e => e.resource === resource);
  if (!entry) return null;

  const params = {};

  if (entry.routeParams) {
    if (entry.routeParams.length === 1) {
      // single param — comes from :id URL slot
      params[entry.routeParams[0]] = id;
    } else {
      // multiple params — come from navigate state
      entry.routeParams.forEach(key => {
        if (state[key] !== undefined) params[key] = state[key];
      });
    }
  }

  return { method: "POST", body: { action: entry.action, params } };
}
