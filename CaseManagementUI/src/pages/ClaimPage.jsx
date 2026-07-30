import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { formatCellValue } from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import api from "../services/http";
import { enrichDocIdLinks, omitColumns } from "../utils/docIdLinks";
import XyzTablePage from "./XyzTablePage";

const DISPLAY_SECTIONS = [
  { key: "claim", label: "Claim for Clearing House" },
  { key: "payer", label: "Payer" },
  { key: "patient", label: "Patient" },
  { key: "authorization", label: "Authorization" },
  { key: "provider", label: "Provider" },
  { key: "practiceConfiguration", label: "Practice Configuration" },
];

export default function ClaimPage() {
  const { caseId } = useParams();
  const navigate = useNavigate();

  const [info, setInfo] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [submitting, setSubmitting] = useState(false);
  const [submitResult, setSubmitResult] = useState(null);
  const [queueRows, setQueueRows] = useState([]);
  const [queueLoading, setQueueLoading] = useState(false);

  const fetchInfo = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/api/corqs", {
        action: "GetClaimInfo",
        params: { caseId: Number(caseId), filter: "Not Claimed" },
      });
      setInfo(res.data?.data ?? {});
      setSelectedIds(new Set());
    } catch (err) {
      console.error("Failed to fetch claim info:", err);
      setError("Failed to fetch claim info.");
      setInfo(null);
    } finally {
      setLoading(false);
    }
  }, [caseId]);

  const fetchQueue = useCallback(async () => {
    setQueueLoading(true);
    try {
      const res = await api.post("/api/corqs", {
        action: "GetClaimQueue",
        params: { caseId: Number(caseId) },
      });
      setQueueRows(res.data?.data ?? []);
    } catch (err) {
      console.error("Failed to fetch claim queue:", err);
      setQueueRows([]);
    } finally {
      setQueueLoading(false);
    }
  }, [caseId]);

  useEffect(() => {
    fetchInfo();
    fetchQueue();
  }, [fetchInfo, fetchQueue]);

  const toggleSession = (jsonDocumentId) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(jsonDocumentId)) next.delete(jsonDocumentId);
      else next.add(jsonDocumentId);
      return next;
    });
  };

  const openClaim = (claimId) => {
    navigate(`/claim/view/${claimId}`);
  };

  const enrichClaimIdLink = (rows) =>
    rows.map((row) => {
      if (row.claimId == null) return row;
      return {
        ...row,
        claimId: (
          <button
            onClick={() => openClaim(row.claimId)}
            className="text-blue-600 underline hover:text-blue-800"
          >
            {row.claimId}
          </button>
        ),
      };
    });

  const handleSubmit = async () => {
    if (selectedIds.size === 0) return;
    setSubmitting(true);
    setSubmitResult(null);
    try {
      const sessionDocumentIds = Array.from(selectedIds).join(",");

      // Everything the claim builder will need, in one linkable doc — provider/payer/
      // authorization/patient are still placeholder-resolved (see usp_GetClaimInfo TODOs)
      // until the case can properly resolve its own peripheral info.
      const spec = {
        caseId: Number(caseId),
        sessions: Array.from(selectedIds),
        provider: info?.provider?.[0]?.jsonDocumentId ?? null,
        authorization: info?.authorization?.[0]?.jsonDocumentId ?? null,
        payer: info?.payer?.[0]?.jsonDocumentId ?? null,
        patient: info?.patient?.[0]?.jsonDocumentId ?? null,
      };
      const specSaveRes = await api.post("/api/saveWorkflow", {
        json: JSON.stringify(spec),
        name: "claim-queue-spec",
      });
      const specDocumentId = specSaveRes.data?.docId ?? null;

      const res = await api.post("/api/corqs", {
        action: "SubmitClaim",
        params: { caseId: Number(caseId), sessionDocumentIds, specDocumentId },
      });
      const queueClaimId = res.data?.data?.[0]?.queueClaimId;
      setSubmitResult({ ok: true, queueClaimId });
      fetchInfo();
      fetchQueue();
    } catch (err) {
      console.error("Failed to submit claim:", err);
      setSubmitResult({ ok: false });
    } finally {
      setSubmitting(false);
    }
  };

  // Always empty/constant here since fetchInfo always filters to "Not Claimed" — these
  // columns only carry information on unfiltered vw_SessionClaimStatus queries.
  const IRRELEVANT_SESSION_COLS = ["claimId", "claimNumber", "claimStatus", "queueClaimId", "sessionClaimStatus"];

  const sessions = info?.sessions ?? [];
  const enrichedSessions = enrichDocIdLinks(sessions, navigate);
  const sessionColumns =
    sessions.length > 0
      ? Object.keys(sessions[0]).filter((c) => !IRRELEVANT_SESSION_COLS.includes(c))
      : [];

  return (
    <div className="p-4 sm:p-6 flex flex-col items-center gap-6">
      <div className="w-full max-w-6xl">
        <PageHeader
          title={`Case Info — Case ${caseId}`}
          breadcrumbs={[
            { label: "Cases", to: "/cases" },
            { label: `Case ${caseId}`, to: `/cases/${caseId}` },
            { label: "Claim 🦪" },
          ]}
        />
        {error && <p className="text-red-500 mb-2">{error}</p>}

        <div className="rounded shadow bg-white border border-gray-200 mb-6">
          <div className="flex items-center justify-between bg-gray-50 border-b border-gray-200 px-4 py-2">
            <h2 className="font-semibold text-lg">
              Sessions — Case {caseId} <span className="text-gray-400 font-normal">({sessions.length})</span>
            </h2>
            <div className="flex items-center gap-2">
              <button
                onClick={fetchInfo}
                className="px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200"
              >
                {loading ? "Loading..." : "Reload"}
              </button>
              <button
                onClick={handleSubmit}
                disabled={selectedIds.size === 0 || submitting}
                className="px-4 py-1 h-9 text-sm rounded-md bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {submitting ? "Submitting..." : `Submit Claim (${selectedIds.size})`}
              </button>
            </div>
          </div>

          {submitResult?.ok && (
            <p className="text-green-600 px-4 py-2">
              Submitted — queue id {submitResult.queueClaimId}. A background job will create the claim.
            </p>
          )}
          {submitResult && !submitResult.ok && (
            <p className="text-red-500 px-4 py-2">Submit failed — see console.</p>
          )}

          <div className="overflow-x-auto">
            {sessions.length === 0 ? (
              <p className="px-4 py-2 text-gray-500">
                {loading ? "Loading..." : "No sessions to create a claim."}
              </p>
            ) : (
              <>
                <p className="px-4 pt-2 text-sm text-gray-500">
                  Select session(s) to create a claim.
                </p>
                <table className="w-full border-collapse border border-gray-300">
                <thead>
                  <tr>
                    <th className="border border-gray-300 px-2 py-1 bg-gray-100"></th>
                    {sessionColumns.map((col) => (
                      <th key={col} className="border border-gray-300 px-2 py-1 text-left bg-gray-100">
                        {col}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {sessions.map((rawRow, i) => {
                    const id = rawRow.jsonDocumentId;
                    const displayRow = enrichedSessions[i];
                    return (
                      <tr key={id ?? i} className={i % 2 === 0 ? "bg-white" : "bg-gray-50"}>
                        <td className="border border-gray-300 px-2 py-1">
                          <input
                            type="checkbox"
                            checked={selectedIds.has(id)}
                            onChange={() => toggleSession(id)}
                          />
                        </td>
                        {sessionColumns.map((col) => (
                          <td className="border border-gray-300 px-2 py-1" key={col}>
                            {formatCellValue(displayRow[col]) ?? ""}
                          </td>
                        ))}
                      </tr>
                    );
                  })}
                </tbody>
                </table>
              </>
            )}
          </div>
        </div>

        <div className="mb-6">
          <XyzTablePage
            title="Queue — Claims To Be Created"
            rows={enrichDocIdLinks(queueRows, navigate)}
            emptyMessage="Nothing queued."
            tableActions={[{ label: queueLoading ? "Loading..." : "Reload", onClick: fetchQueue }]}
          />
        </div>

        {DISPLAY_SECTIONS.map(({ key, label }) => {
          let rows = enrichDocIdLinks(info?.[key] ?? [], navigate);
          if (key === "claim") rows = enrichClaimIdLink(rows);
          if (key === "payer") rows = omitColumns(rows, ["publicId"]);
          return (
            <div key={key} className="mb-6">
              <XyzTablePage title={label} rows={rows} emptyMessage={`No ${label.toLowerCase()} found.`} />
            </div>
          );
        })}
      </div>
    </div>
  );
}
