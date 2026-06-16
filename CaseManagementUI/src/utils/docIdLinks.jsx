const DOC_ID_COLS = ["documentId", "sourceDocumentId", "jsonDocumentId"];

export function enrichDocIdLinks(rows, navigate) {
  return rows.map((row) => {
    const patched = { ...row };
    for (const col of DOC_ID_COLS) {
      const id = patched[col];
      if (id != null)
        patched[col] = (
          <button
            onClick={() => navigate(`/docviewer/${id}`)}
            className="text-blue-600 underline hover:text-blue-800"
          >
            {id}
          </button>
        );
    }
    return patched;
  });
}
