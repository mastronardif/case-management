import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/http";
import XyzTablePage from "./XyzTablePage";

const DISPLAY_SECTIONS = [
  { key: "claim", label: "Claim" },
  { key: "payer", label: "Payer" },
  { key: "patient", label: "Patient" },
  { key: "authorization", label: "Authorization" },
  { key: "provider", label: "Provider" },
  { key: "practiceConfiguration", label: "Practice Configuration" },
];

export default function ClaimPage() {
  const { caseId } = useParams();

  const [info, setInfo] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [submitting, setSubmitting] = useState(false);
  const [submitResult, setSubmitResult] = useState(null);

  const fetchInfo = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/api/corqs", {
        action: "GetClaimInfo",
        params: { caseId: Number(caseId) },
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

  useEffect(() => {
    fetchInfo();
  }, [fetchInfo]);

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
      const res = await api.post("/api/corqs", {
        action: "SubmitClaim",
        params: { caseId: Number(caseId), sessionDocumentIds },
      });
      const queueClaimId = res.data?.data?.[0]?.queueClaimId;
      setSubmitResult({ ok: true, queueClaimId });
      fetchInfo();
    } catch (err) {
      console.error("Failed to submit claim:", err);
      setSubmitResult({ ok: false });
    } finally {
      setSubmitting(false);
    }
  };

  const sessions = info?.sessions ?? [];
  const sessionColumns = sessions.length > 0 ? Object.keys(sessions[0]) : [];

  return (
    <div className="p-4 sm:p-6 flex flex-col items-center gap-6">
      <div className="w-full max-w-6xl">
        <h1 className="text-xl font-semibold mb-4">Case Info — Case {caseId}</h1>
        {error && <p className="text-red-500 mb-2">{error}</p>}

        <div className="rounded shadow bg-white border border-gray-200 mb-6">
          <div className="flex items-center justify-between bg-gray-50 border-b border-gray-200 px-4 py-2">
            <h2 className="font-semibold text-lg">Sessions — Case {caseId}</h2>
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
                {loading ? "Loading..." : "No sessions found."}
              </p>
            ) : (
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
                  {sessions.map((row, i) => {
                    const id = row.jsonDocumentId;
                    const alreadyClaimed = row.claimId != null;
                    return (
                      <tr key={id ?? i} className={i % 2 === 0 ? "bg-white" : "bg-gray-50"}>
                        <td className="border border-gray-300 px-2 py-1">
                          <input
                            type="checkbox"
                            checked={selectedIds.has(id)}
                            disabled={alreadyClaimed}
                            onChange={() => toggleSession(id)}
                          />
                        </td>
                        {sessionColumns.map((col) => (
                          <td className="border border-gray-300 px-2 py-1" key={col}>
                            {String(row[col] ?? "")}
                          </td>
                        ))}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>
        </div>

        {DISPLAY_SECTIONS.map(({ key, label }) => (
          <div key={key} className="mb-6">
            <XyzTablePage title={label} rows={info?.[key] ?? []} emptyMessage={`No ${label.toLowerCase()} found.`} />
          </div>
        ))}
      </div>
    </div>
  );
}
