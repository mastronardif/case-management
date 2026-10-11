import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { formatCellValue } from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import api from "../services/http";
import { enrichDocIdLinks, omitColumns } from "../utils/docIdLinks";
import XyzTablePage from "./XyzTablePage";

const DISPLAY_SECTIONS = [
  { key: "claim", label: "Claim for Clearing House" },
  { key: "insuranceCoverage", label: "Insurance Coverage" },
  { key: "payer", label: "Payer" },
  { key: "patient", label: "Patient" },
  { key: "authorization", label: "Authorization" },
  { key: "feeSchedule", label: "Fee Schedule" },
  { key: "provider", label: "Provider" },
  { key: "practiceConfiguration", label: "Practice Configuration" },
];

function DocLink({ id, navigate }) {
  if (id == null) return null;
  return (
    <button onClick={() => navigate(`/docviewer/${id}`)} className="text-blue-600 underline hover:text-blue-800">
      {id}
    </button>
  );
}

// The two docs always go together — one PriorAuthForm row paired with one TreatmentPlanAuthorization
// row, same bundle a submission needs. Both lists come back sorted newest-first, so pairing by
// position lines up the PA and TPA that were actually filled out together; one checkbox per pair
// selects both ids at once instead of two separate single-select tables.
function BundleSelectTable({ paRows, tpaRows, selectedPaId, onSelect, navigate, emptyMessage }) {
  const pairCount = Math.min(paRows.length, tpaRows.length);
  if (pairCount === 0) return <p className="px-4 py-2 text-gray-500">{emptyMessage}</p>;

  const pairs = Array.from({ length: pairCount }, (_, i) => ({ pa: paRows[i], tpa: tpaRows[i] }));
  const nowrap = "border border-gray-300 px-2 py-1 whitespace-nowrap";

  return (
    <>
      <table className="w-full border-collapse border border-gray-300">
        <thead>
          <tr>
            <th className={`${nowrap} bg-gray-100`}></th>
            <th className={`${nowrap} text-left bg-gray-100`}>PA Id</th>
            <th className={`${nowrap} text-left bg-gray-100`}>TPA Id</th>
            <th className={`${nowrap} text-left bg-gray-100`}>Patient</th>
            <th className={`${nowrap} text-left bg-gray-100`}>DOB</th>
            <th className={`${nowrap} text-left bg-gray-100`}>Member ID</th>
            <th className={`${nowrap} text-left bg-gray-100`}>Diagnosis</th>
            <th className={`${nowrap} text-left bg-gray-100`}>PA Provider</th>
            <th className={`${nowrap} text-left bg-gray-100`}>TPA Provider (NPI)</th>
            <th className={`${nowrap} text-left bg-gray-100`}>Auth #</th>
            <th className={`${nowrap} text-left bg-gray-100`}>PA Doc</th>
            <th className={`${nowrap} text-left bg-gray-100`}>TPA Doc</th>
          </tr>
        </thead>
        <tbody>
          {pairs.map(({ pa, tpa }, i) => (
            <tr key={pa.id} className={i % 2 === 0 ? "bg-white" : "bg-gray-50"}>
              <td className={nowrap}>
                <input type="checkbox" checked={selectedPaId === pa.id} onChange={() => onSelect(pa, tpa)} />
              </td>
              <td className={nowrap}>{pa.id}</td>
              <td className={nowrap}>{tpa.id}</td>
              <td className={nowrap}>{formatCellValue(pa.patientName) ?? ""}</td>
              <td className={nowrap}>{formatCellValue(pa.patientDOB) ?? ""}</td>
              <td className={nowrap}>{formatCellValue(pa.memberId) ?? ""}</td>
              <td className={nowrap}>{formatCellValue(pa.diagnosisCode) ?? ""}</td>
              <td className={nowrap}>{formatCellValue(pa.providerName) ?? ""}</td>
              <td className={nowrap}>
                {formatCellValue(tpa.providerName) ?? ""}{tpa.providerNPI ? ` (${tpa.providerNPI})` : ""}
              </td>
              <td className={nowrap}>{formatCellValue(pa.authorizationNumber) ?? ""}</td>
              <td className={nowrap}><DocLink id={pa.sourceDocumentId} navigate={navigate} /></td>
              <td className={nowrap}><DocLink id={tpa.sourceDocumentId} navigate={navigate} /></td>
            </tr>
          ))}
        </tbody>
      </table>
      {paRows.length !== tpaRows.length && (
        <p className="px-4 py-2 text-amber-600 text-sm">
          {paRows.length} Prior Auth Form(s) but {tpaRows.length} Treatment Plan Authorization(s) —
          only the first {pairCount} paired by most-recent are shown as bundles.
        </p>
      )}
    </>
  );
}

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

  const [paForms, setPaForms] = useState([]);
  const [tpaForms, setTpaForms] = useState([]);
  const [authDocsLoading, setAuthDocsLoading] = useState(false);
  const [selectedPaId, setSelectedPaId] = useState(null);
  const [selectedTpaId, setSelectedTpaId] = useState(null);
  const [authSubmitting, setAuthSubmitting] = useState(false);
  const [authSubmitResult, setAuthSubmitResult] = useState(null);
  const [authQueueRows, setAuthQueueRows] = useState([]);
  const [authQueueLoading, setAuthQueueLoading] = useState(false);

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

  const fetchAuthDocs = useCallback(async () => {
    setAuthDocsLoading(true);
    try {
      const [paRes, tpaRes] = await Promise.all([
        api.post("/api/corqs", { action: "getPriorAuthFormsForCase", params: { caseId: Number(caseId) } }),
        api.post("/api/corqs", { action: "getTreatmentPlanAuthorizationsForCase", params: { caseId: Number(caseId) } }),
      ]);
      setPaForms(paRes.data?.data ?? []);
      setTpaForms(tpaRes.data?.data ?? []);
      setSelectedPaId(null);
      setSelectedTpaId(null);
    } catch (err) {
      console.error("Failed to fetch prior auth docs:", err);
      setPaForms([]);
      setTpaForms([]);
    } finally {
      setAuthDocsLoading(false);
    }
  }, [caseId]);

  const fetchAuthQueue = useCallback(async () => {
    setAuthQueueLoading(true);
    try {
      const res = await api.post("/api/corqs", {
        action: "GetAuthDocsQueue",
        params: { caseId: Number(caseId) },
      });
      setAuthQueueRows(res.data?.data ?? []);
    } catch (err) {
      console.error("Failed to fetch auth docs queue:", err);
      setAuthQueueRows([]);
    } finally {
      setAuthQueueLoading(false);
    }
  }, [caseId]);

  useEffect(() => {
    fetchInfo();
    fetchQueue();
    fetchAuthDocs();
    fetchAuthQueue();
  }, [fetchInfo, fetchQueue, fetchAuthDocs, fetchAuthQueue]);

  const handleSubmitAuthDocs = async () => {
    if (!selectedPaId || !selectedTpaId) return;
    setAuthSubmitting(true);
    setAuthSubmitResult(null);
    try {
      const res = await api.post("/api/corqs", {
        action: "SubmitAuthDocs",
        params: {
          caseId: Number(caseId),
          priorAuthFormId: selectedPaId,
          treatmentPlanAuthorizationId: selectedTpaId,
        },
      });
      const queueSubmitId = res.data?.data?.[0]?.queueSubmitId;
      setAuthSubmitResult({ ok: true, queueSubmitId });
      fetchAuthDocs();
      fetchAuthQueue();
    } catch (err) {
      console.error("Failed to submit auth docs:", err);
      setAuthSubmitResult({ ok: false });
    } finally {
      setAuthSubmitting(false);
    }
  };

  const toggleSession = (jsonDocumentId) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(jsonDocumentId)) next.delete(jsonDocumentId);
      else next.add(jsonDocumentId);
      return next;
    });
  };

  const handleSubmit = async () => {
    if (selectedIds.size === 0) return;
    setSubmitting(true);
    setSubmitResult(null);
    try {
      const sessionDocumentIds = Array.from(selectedIds).join(",");

      // Everything the claim builder will need, in one linkable doc — provider/authorization/
      // patient are still placeholder-resolved (see usp_GetClaimInfo TODOs) until the case can
      // properly resolve its own peripheral info. Payer isn't included here — it's a business
      // entity resolved directly via InsuranceCoverage.PayerId inside the build scripts, not a
      // doc-id lookup (cases.Payer no longer carries SourceDocumentId/JsonDocumentId).
      const spec = {
        caseId: Number(caseId),
        sessions: Array.from(selectedIds),
        provider: info?.provider?.[0]?.jsonDocumentId ?? null,
        authorization: info?.authorization?.[0]?.jsonDocumentId ?? null,
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

        <div className="rounded shadow bg-white border border-gray-200 mb-6">
          <div className="flex items-center justify-between bg-gray-50 border-b border-gray-200 px-4 py-2">
            <h2 className="font-semibold text-lg">
              Prior Authorization Submission{" "}
              <span className="text-gray-400 font-normal text-base">(PriorAuthForm, TreatmentPlanAuthorization)</span>
              {" "}— Case {caseId}{" "}
              <span className="text-gray-400 font-normal">({Math.min(paForms.length, tpaForms.length)})</span>
            </h2>
            <div className="flex items-center gap-2">
              <button
                onClick={fetchAuthDocs}
                className="px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200"
              >
                {authDocsLoading ? "Loading..." : "Reload"}
              </button>
              <button
                onClick={handleSubmitAuthDocs}
                disabled={!selectedPaId || !selectedTpaId || authSubmitting}
                className="px-4 py-1 h-9 text-sm rounded-md bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {authSubmitting ? "Submitting..." : "Submit to Evernorth (bundle)"}
              </button>
            </div>
          </div>

          {authSubmitResult?.ok && (
            <p className="text-green-600 px-4 py-2">
              Submitted — queue id {authSubmitResult.queueSubmitId}. Status starts Pending until the
              (mocked) send step runs.
            </p>
          )}
          {authSubmitResult && !authSubmitResult.ok && (
            <p className="text-red-500 px-4 py-2">Submit failed — see console.</p>
          )}

          <div className="overflow-x-auto">
            <p className="px-4 pt-2 pb-2 text-sm text-gray-500">
              Select a bundle to submit — the two docs always go together, so one checkbox picks both.
            </p>
            <BundleSelectTable
              paRows={paForms}
              tpaRows={tpaForms}
              selectedPaId={selectedPaId}
              onSelect={(pa, tpa) => { setSelectedPaId(pa.id); setSelectedTpaId(tpa.id); }}
              navigate={navigate}
              emptyMessage={authDocsLoading ? "Loading..." : "No Prior Auth / Treatment Plan docs for this case."}
            />
          </div>
        </div>

        <div className="mb-6">
          <XyzTablePage
            title="Queue — Auth Docs To Be Submitted"
            rows={enrichDocIdLinks(authQueueRows, navigate)}
            emptyMessage="Nothing queued."
            tableActions={[{ label: authQueueLoading ? "Loading..." : "Reload", onClick: fetchAuthQueue }]}
          />
        </div>

        {DISPLAY_SECTIONS.map(({ key, label }) => {
          let rows = enrichDocIdLinks(info?.[key] ?? [], navigate);
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
