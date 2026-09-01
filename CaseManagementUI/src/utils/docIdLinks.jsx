const DOC_ID_COLS = ["sourceDocumentId", "jsonDocumentId", "ediDocumentId", "availityReviewDocumentId", "documentId", "sourcesDocumentId", "specDocumentId", "jId", "src"];
// Columns holding a comma-joined list of doc ids (e.g. usp_GetClaimQueue's STRING_AGG'd
// SessionDocumentIds) rather than a single id — each id in the list gets its own link.
const MULTI_DOC_ID_COLS = ["sessionDocumentIds"];

export function omitColumns(rows, cols) {
  return rows.map((row) => {
    const patched = { ...row };
    for (const col of cols) delete patched[col];
    return patched;
  });
}

function docIdLink(id, navigate) {
  return (
    <button
      onClick={() => navigate(`/docviewer/${id}`)}
      className="text-blue-600 underline hover:text-blue-800"
    >
      {id}
    </button>
  );
}

export function enrichDocIdLinks(rows, navigate) {
  return rows.map((row) => {
    const patched = { ...row };
    for (const col of DOC_ID_COLS) {
      const id = patched[col];
      if (id != null) patched[col] = docIdLink(id, navigate);
    }
    for (const col of MULTI_DOC_ID_COLS) {
      const value = patched[col];
      if (value) {
        const ids = String(value).split(",").map((s) => s.trim()).filter(Boolean);
        // DataTable's renderValue only renders raw React elements directly (via
        // React.isValidElement) — a bare array of elements isn't one, so it falls through to
        // JSON.stringify, which throws on elements (functions/circular refs) and shows
        // "[object]". Wrap the list in one actual element.
        patched[col] = (
          <span>
            {ids.map((id, i) => (
              <span key={id}>
                {i > 0 && ", "}
                {docIdLink(id, navigate)}
              </span>
            ))}
          </span>
        );
      }
    }
    return patched;
  });
}
