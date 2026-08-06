import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import PageHeader from "../components/PageHeader";
import api from "../services/http";
import { enrichDocIdLinks, omitColumns } from "../utils/docIdLinks";
import XyzTablePage from "./XyzTablePage";

const DISPLAY_SECTIONS = [
  { key: "insuranceCoverage", label: "Insurance Coverage" },
  { key: "payer", label: "Payer" },
  { key: "patient", label: "Patient" },
  { key: "authorization", label: "Authorization" },
  { key: "provider", label: "Provider" },
  { key: "practiceConfiguration", label: "Practice Configuration" },
];

export default function ClaimViewPage() {
  const { claimId } = useParams();
  const navigate = useNavigate();

  const [info, setInfo] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchInfo = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/api/corqs", {
        action: "GetClaimInfo",
        params: { claimId: Number(claimId) },
      });
      setInfo(res.data?.data ?? {});
    } catch (err) {
      console.error("Failed to fetch claim:", err);
      setError("Failed to fetch claim.");
      setInfo(null);
    } finally {
      setLoading(false);
    }
  }, [claimId]);

  useEffect(() => {
    fetchInfo();
  }, [fetchInfo]);

  const claim = info?.claim?.[0];
  const sessions = info?.sessions ?? [];

  return (
    <div className="p-4 sm:p-6 flex flex-col items-center gap-6">
      <div className="w-full max-w-6xl">
        <PageHeader
          title={`Claim ${claimId}`}
          breadcrumbs={[{ label: "Claim Queue", to: "/claim" }, { label: `Claim ${claimId}` }]}
          actions={[{ label: loading ? "Loading..." : "Reload", onClick: fetchInfo }]}
        />
        {error && <p className="text-red-500 mb-2">{error}</p>}

        <div className="mb-6">
          <XyzTablePage
            title="Claim"
            rows={claim ? enrichDocIdLinks([claim], navigate) : []}
            emptyMessage={loading ? "Loading..." : "Claim not found."}
          />
        </div>

        <div className="mb-6">
          <XyzTablePage
            title="Sessions"
            rows={enrichDocIdLinks(sessions, navigate)}
            emptyMessage="No sessions found."
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
