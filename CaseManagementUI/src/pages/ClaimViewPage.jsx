import { useParams } from "react-router-dom";
import PageHeader from "../components/PageHeader";

export default function ClaimViewPage() {
  const { claimId } = useParams();

  return (
    <div className="p-4 sm:p-6 flex flex-col items-center gap-6">
      <div className="w-full max-w-6xl">
        <PageHeader
          title={`Claim ${claimId}`}
          breadcrumbs={[{ label: "Claim Queue", to: "/claim" }, { label: `Claim ${claimId}` }]}
        />
        <div className="rounded shadow bg-white border border-gray-200 p-6 text-gray-500">
          TBD claimId({claimId})
        </div>
      </div>
    </div>
  );
}
