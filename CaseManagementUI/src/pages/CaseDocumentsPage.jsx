import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";
import { enrichDocIdLinks } from "../utils/docIdLinks";
import { enrichRows } from "../utils/documentEnrichment";
import XyzTablePage from "./XyzTablePage";

const SESSION_SCHEMA = QUERY_MAP.find((e) => e.resource === "getSessionList");

const openButtonClass = "px-3 py-1 text-sm rounded bg-green-500 text-white hover:bg-green-600";

// Adds an Action column that opens the raw (pre-enrichment) row, since enrichDocIdLinks
// replaces id columns with link buttons and DataTable renders whatever's under "Action" as-is.
const withOpenAction = (rawRows, navigate, onOpen) =>
  enrichDocIdLinks(rawRows, navigate).map((displayRow, i) => ({
    ...displayRow,
    Action: (
      <button className={openButtonClass} onClick={() => onOpen(rawRows[i])}>
        Open
      </button>
    ),
  }));

export default function CaseDocumentsPage() {
  const { caseId } = useParams();
  const navigate = useNavigate();

  const [docRowsRaw, setDocRowsRaw] = useState([]);
  const [docLoading, setDocLoading] = useState(false);
  const [docError, setDocError] = useState(null);

  const [sessionRowsRaw, setSessionRowsRaw] = useState([]);
  const [sessionLoading, setSessionLoading] = useState(false);
  const [sessionError, setSessionError] = useState(null);

  const fetchDocs = useCallback(async () => {
    setDocLoading(true);
    setDocError(null);
    try {
      const res = await apiFetch("/api/corqs", { action: "Case_GetDocuments", params: { caseId: Number(caseId) } });
      const raw = Array.isArray(res) ? res : res?.data ?? [];
      setDocRowsRaw(raw);
    } catch {
      setDocError("Failed to fetch documents.");
      setDocRowsRaw([]);
    } finally {
      setDocLoading(false);
    }
  }, [caseId]);

  const fetchSessions = useCallback(async () => {
    setSessionLoading(true);
    setSessionError(null);
    try {
      const res = await apiFetch("/api/corqs", { action: "getSessionList", params: { caseId: Number(caseId) } });
      const raw = Array.isArray(res) ? res : res?.data ?? [];
      const enriched = await enrichRows(raw, SESSION_SCHEMA?.enrichments);
      setSessionRowsRaw(enriched);
    } catch {
      setSessionError("Failed to fetch sessions.");
      setSessionRowsRaw([]);
    } finally {
      setSessionLoading(false);
    }
  }, [caseId]);

  useEffect(() => { fetchDocs(); }, [fetchDocs]);
  useEffect(() => { fetchSessions(); }, [fetchSessions]);

  const openSession = (sessionId) => {
    alert(`TBD: openSession(${sessionId})`);
  };

  const openDocument = (documentId, documentType) => {
    alert(`TBD: openDocument(${documentId}, ${documentType})`);
  };

  const sessionRows = useMemo(
    () => withOpenAction(sessionRowsRaw, navigate, (row) => openSession(row.jsonDocumentId)),
    [sessionRowsRaw, navigate]
  );
  const docRows = useMemo(
    () => withOpenAction(docRowsRaw, navigate, (row) => openDocument(row.documentId, row.documentType)),
    [docRowsRaw, navigate]
  );

  const docTableActions = [{ label: docLoading ? "Loading..." : "Reload", onClick: fetchDocs }];
  const sessionTableActions = [{ label: sessionLoading ? "Loading..." : "Reload", onClick: fetchSessions }];

  return (
    <div>
      <XyzTablePage
        title={`Sessions — Case ${caseId}`}
        rows={sessionRows}
        tableActions={sessionTableActions}
      />
      {sessionError && <p className="text-red-500 mt-2 px-6">{sessionError}</p>}

      <XyzTablePage
        title={`Documents — Case ${caseId}`}
        rows={docRows}
        tableActions={docTableActions}
      />
      {docError && <p className="text-red-500 mt-2 px-6">{docError}</p>}
    </div>
  );
}
