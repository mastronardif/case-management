import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import PageHeader from "../components/PageHeader";
import api from "../services/http";
import { enrichDocIdLinks, omitColumns } from "../utils/docIdLinks";
import XyzTablePage from "./XyzTablePage";

// Same shape as ClaimPage's GetClaimInfo-backed sections, minus the claim-submission pieces
// (session selection, Submit Claim, Queue) — this page is for reviewing a case's workbook,
// not building a claim. Sessions/Documents get their own state since ClaimPage.jsx doesn't
// need enrichRows(patient name) style treatment here — plain XyzTablePage suffices.
const STATIC_SECTIONS = [
  { key: "insuranceCoverage", label: "Insurance Coverage" },
  { key: "payer", label: "Payer" },
  { key: "patient", label: "Patient" },
  { key: "authorization", label: "Authorization" },
];

// Always empty/constant here since GetClaimInfo is always called with filter="Not Claimed" —
// these columns only carry information on unfiltered vw_SessionClaimStatus queries.
const IRRELEVANT_SESSION_COLS = ["claimId", "claimNumber", "claimStatus", "queueClaimId", "sessionClaimStatus"];

export default function WorkbooksPage() {
  const { caseId } = useParams();
  const navigate = useNavigate();

  const [info, setInfo] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [documents, setDocuments] = useState([]);
  const [documentsLoading, setDocumentsLoading] = useState(false);

  const fetchInfo = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/api/corqs", {
        action: "GetClaimInfo",
        params: { caseId: Number(caseId), filter: "Not Claimed" },
      });
      setInfo(res.data?.data ?? {});
    } catch (err) {
      console.error("Failed to fetch case info:", err);
      setError("Failed to fetch case info.");
      setInfo(null);
    } finally {
      setLoading(false);
    }
  }, [caseId]);

  const fetchDocuments = useCallback(async () => {
    setDocumentsLoading(true);
    try {
      const res = await api.post("/api/corqs", {
        action: "Case_GetDocuments",
        params: { caseId: String(caseId) },
      });
      setDocuments(res.data?.data ?? []);
    } catch (err) {
      console.error("Failed to fetch case documents:", err);
      setDocuments([]);
    } finally {
      setDocumentsLoading(false);
    }
  }, [caseId]);

  useEffect(() => {
    fetchInfo();
    fetchDocuments();
  }, [fetchInfo, fetchDocuments]);

  const sessions = omitColumns(enrichDocIdLinks(info?.sessions ?? [], navigate), IRRELEVANT_SESSION_COLS);

  return (
    <div className="p-4 sm:p-6 flex flex-col items-center gap-6">
      <div className="w-full max-w-6xl">
        <PageHeader
          title={`Workbooks — Case ${caseId}`}
          breadcrumbs={[
            { label: "Cases", to: "/cases" },
            { label: `Case ${caseId}`, to: `/cases/${caseId}` },
            { label: "Workbooks" },
          ]}
        />
        {error && <p className="text-red-500 mb-2">{error}</p>}

        <div className="mb-6">
          <XyzTablePage
            title="Sessions"
            rows={sessions}
            emptyMessage="No unclaimed sessions."
            tableActions={[{ label: loading ? "Loading..." : "Reload", onClick: fetchInfo }]}
          />
        </div>

        {STATIC_SECTIONS.map(({ key, label }) => {
          let rows = enrichDocIdLinks(info?.[key] ?? [], navigate);
          if (key === "payer") rows = omitColumns(rows, ["publicId"]);
          return (
            <div key={key} className="mb-6">
              <XyzTablePage title={label} rows={rows} emptyMessage={`No ${label.toLowerCase()} found.`} />
            </div>
          );
        })}

        <div className="mb-6">
          <XyzTablePage
            title="Documents"
            rows={enrichDocIdLinks(documents, navigate)}
            emptyMessage="No documents found."
            tableActions={[{ label: documentsLoading ? "Loading..." : "Reload", onClick: fetchDocuments }]}
          />
        </div>
      </div>
    </div>
  );
}
