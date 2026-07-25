import PageHeader from "../components/PageHeader";

export default function ClaimQueuePage() {
  return (
    <div className="p-4 sm:p-6 flex flex-col items-center gap-6">
      <div className="w-full max-w-6xl">
        <PageHeader
          title="Claim Queue"
          breadcrumbs={[{ label: "Claim Queue 🦪" }]}
        />
        <div className="rounded shadow bg-white border border-gray-200 p-6 text-gray-500">
          TBD — list all claims.
        </div>
      </div>
    </div>
  );
}
