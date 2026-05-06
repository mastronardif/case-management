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
    }

    if (resource === "getWorkbooksByCase") {
        if (type === "caseId") {
        return {
        method: "POST",
        body: {
            action: "getWorkbooksByCase",
            params: {caseId: Number(id)}
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

    if (resource === "calendar") {
    return {
      method: "POST",
      body: {
        action: "getCalendar",
        params: {month: Number(id)}
      }
    };
  }

  // fallback
  return null;
}
