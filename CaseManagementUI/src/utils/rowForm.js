// Opens a single already-fetched row as a labeled-field form view (/rowform). Transient by
// design: the row travels via router state, nothing is persisted or re-fetched — the same
// pattern CasePage.jsx already uses to pass caseData around. A hard refresh on /rowform loses
// the data, same as a refresh loses CasePage's caseData; there's no fallback re-fetch here
// either, on purpose.
export function openRowForm(navigate, title, row) {
  navigate("/rowform", { state: { title, row } });
}
