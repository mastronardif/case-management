import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { apiFetch } from "../services/apiFetch";
import { QUERY_MAP } from "../utils/corqsreact";
import { enrichDocIdLinks } from "../utils/docIdLinks";
import { enrichRows } from "../utils/documentEnrichment";
import XyzTablePage from "./XyzTablePage";

const SESSION_SCHEMA = QUERY_MAP.find((e) => e.resource === "getSessionList");

const RowActions = ({ row, actions }) => (
  <div className="flex gap-1">
    {actions.map((action, i) => (
      <button
        key={i}
        onClick={() => action.onClick(row)}
        className={action.className || "px-3 py-1 text-sm rounded bg-green-500 text-white hover:bg-green-600"}
      >
        {action.label}
      </button>
    ))}
  </div>
);

export default function CaseDocumentsPage() {
  const { caseId } = useParams();
  const navigate = useNavigate();

  const [docRows, setDocRows] = useState([]);
  const [docLoading, setDocLoading] = useState(false);
  const [docError, setDocError] = useState(null);

  const [sessionRows, setSessionRows] = useState([]);
  const [sessionLoading, setSessionLoading] = useState(false);
  const [sessionError, setSessionError] = useState(null);

  const fetchDocs = useCallback(async () => {
    setDocLoading(true);
    setDocError(null);
    try {
      const res = await apiFetch("/api/corqs", { action: "Case_GetDocuments", params: { caseId: Number(caseId) } });
      const raw = Array.isArray(res) ? res : res?.data ?? [];
      setDocRows(enrichDocIdLinks(raw, navigate));
    } catch {
      setDocError("Failed to fetch documents.");
      setDocRows([]);
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
      setSessionRows(enrichDocIdLinks(enriched, navigate));
    } catch {
      setSessionError("Failed to fetch sessions.");
      setSessionRows([]);
    } finally {
      setSessionLoading(false);
    }
  }, [caseId]);

  useEffect(() => { fetchDocs(); }, [fetchDocs]);
  useEffect(() => { fetchSessions(); }, [fetchSessions]);

  const openAction = (row) => navigate(`/docviewer/${row.documentId}`);

  const docRowActions = [{ label: "Open", onClick: openAction }];
  const sessionRowActions = [{ label: "Open", onClick: openAction }];

  const docTableActions = [{ label: docLoading ? "Loading..." : "Reload", onClick: fetchDocs }];
  const sessionTableActions = [{ label: sessionLoading ? "Loading..." : "Reload", onClick: fetchSessions }];

  return (
    <div>
            <XyzTablePage
        title={`Sessions — Case ${caseId}`}
        rows={sessionRows}
        ActionRowComponent={RowActions}
        rowActions={sessionRowActions}
        tableActions={sessionTableActions}
      />
      {sessionError && <p className="text-red-500 mt-2 px-6">{sessionError}</p>}

      <XyzTablePage
        title={`Documents — Case ${caseId}`}
        rows={docRows}
        ActionRowComponent={RowActions}
        rowActions={docRowActions}
        tableActions={docTableActions}
      />
      {docError && <p className="text-red-500 mt-2 px-6">{docError}</p>}
    </div>
  );
}
