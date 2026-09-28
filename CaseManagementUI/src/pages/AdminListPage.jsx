import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ActionRow from "../components/ActionRow";
import api from "../services/http";
import { openRowForm } from "../utils/rowForm";
import XyzTablePage from "./XyzTablePage";

// Static/reference entities — system-wide lists (every case, not just one), each backed by a
// cases.vw_*Admin view (join for CaseNumber etc. lives in SQL, not here). Read-only for now:
// this data mostly comes from an authorized source (payer letters, intake forms), so editing it
// is a separate, more careful piece of work for later.
export const ADMIN_ENTITIES = {
  payer: { action: "listPayers", label: "Payer" },
  insuranceCoverage: { action: "listInsuranceCoverage", label: "Insurance Coverage" },
  patient: { action: "listPatients", label: "Patient" },
  authorization: { action: "listAuthorizations", label: "Authorization" },
};

export default function AdminListPage({ resource }) {
  const navigate = useNavigate();
  const entity = ADMIN_ENTITIES[resource];

  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    if (!entity) return;
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/api/corqs", { action: entity.action, params: {} });
      setRows(res.data?.data ?? []);
    } catch (err) {
      console.error(`Failed to fetch ${entity?.label}:`, err);
      setError(`Failed to fetch ${entity.label.toLowerCase()}.`);
      setRows([]);
    } finally {
      setLoading(false);
    }
  }, [entity]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  if (!entity) {
    return <p className="p-6 text-red-500">Unknown admin entity "{resource}".</p>;
  }

  const rowActions = [{ label: "Form", onClick: (row) => openRowForm(navigate, entity.label, row) }];

  return (
    <div className="p-4 sm:p-6">
      {error && <p className="text-red-500 mb-2">{error}</p>}
      <XyzTablePage
        title={entity.label}
        rows={rows}
        ActionRowComponent={ActionRow}
        rowActions={rowActions}
        tableActions={[{ label: loading ? "Loading..." : "Reload", onClick: fetchData }]}
        emptyMessage={`No ${entity.label.toLowerCase()} found.`}
      />
    </div>
  );
}
