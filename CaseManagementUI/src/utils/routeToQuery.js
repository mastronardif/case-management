export function buildQuery(resource, type, id) {
  if (resource === "workbooks") {
    if (!type) {
      return {
        method: "POST",
        body: {
          action: "getAllWorkbooks",
          params: {}
        }
      };
    }

    if (type === "case") {
      return {
        method: "POST",
        body: {
          action: "getWorkbooksByCase",
          params: { caseId: Number(id) }
        }
      };
    }
  }

  if (resource === "cases") {
    return {
      method: "POST",
      body: {
        action: "searchCases",
        params: {}
      }
    };
  }

  // fallback
  return null;
}
