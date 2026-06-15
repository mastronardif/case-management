import api from "../services/http";

// Cache of fetched document JSON, keyed by docId. Shared across calls so
// repeated renders/pages don't re-fetch the same document.
const documentCache = new Map();

function fetchDocument(docId) {
  if (!documentCache.has(docId)) {
    documentCache.set(
      docId,
      api
        .get("/api/getDocument", { params: { docId } })
        .then((res) => res.data)
        .catch(() => null)
    );
  }
  return documentCache.get(docId);
}

function getByPath(obj, path) {
  return path
    .split(".")
    .reduce((acc, key) => (acc == null ? undefined : acc[key]), obj);
}

/**
 * Adds derived columns to rows by reading values out of JSON documents
 * referenced by a column on each row (e.g. jsonDocumentId).
 *
 * @param {Array<object>} rows
 * @param {Array<{ column: string, sourceColumn: string, path: string }>} enrichments
 * @returns {Promise<Array<object>>}
 */
export async function enrichRows(rows, enrichments) {
  if (!rows?.length || !enrichments?.length) return rows;

  const docIds = new Set();
  for (const row of rows) {
    for (const { sourceColumn } of enrichments) {
      const docId = row[sourceColumn];
      if (docId != null) docIds.add(docId);
    }
  }

  const docs = new Map();
  await Promise.all(
    [...docIds].map(async (docId) => {
      docs.set(docId, await fetchDocument(docId));
    })
  );

  return rows.map((row) => {
    const enriched = { ...row };
    for (const { column, sourceColumn, path } of enrichments) {
      const docId = row[sourceColumn];
      const doc = docId != null ? docs.get(docId) : null;
      enriched[column] = doc ? getByPath(doc, path) ?? "" : "";
    }
    return enriched;
  });
}
