// CasePage.jsx
import { useLocation, useNavigate, useParams } from "react-router-dom";
import ActionPage from "../components/ActionPage";
import PageHeader from "../components/PageHeader";
import { setTableActions } from "../utils/tableActionStore";

export default function CasePage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const caseData = state?.caseData;
  const { caseId } = useParams();
  const id = caseId ?? "new";
  const pdfUrl = `/api/getDocument?caseId=${id}&documentType=IntakeForm`;
  // /api/corqs/getDocument?documentId=4

  const greyButtonClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed";

 

  const pageActions = [
    { label: "Work Books", onClick: () => handleWorkbooks() },
    { label: "RBT Books", onClick: () => handleRBTbooks() },
    { label: "Insurance Books", onClick: () => handleInsurancebooks() },
    { label: "Import from Scan", onClick: () => handleImportScan() },
    { label: "Claims 🦪", onClick: () => handleFillForm() },
  ];

  const handleWorkbooks = () => {
    navigate(`/workbooks/${id}`, { state: { caseData } });
  };

  const handleRBTbooks = () => {
    setTableActions("getWorkbooksByCase", [
      {
        label: "Audit",
        onClick: () => alert("Audit coming soon."),
        className: greyButtonClass,
      },
    ]);
    navigate(`/data/getWorkbooksByCase/caseId/${id}`, { state: { caseData } });
  };

  const handleInsurancebooks = () => {
    navigate(`/table`, { state: { caseData } });
  };

  const handleImportScan = () => {
    alert("Import from scan feature coming soon!");
  };

  const handleFillForm = () => {
    navigate(`/claim/${id}`, { state: { caseData } });
    //alert("Fill out form feature coming soon!");
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-start p-6 bg-gray-50">
      <div className="w-full max-w-4xl">
        <PageHeader
          title={`Case ${id}`}
          breadcrumbs={[
            { label: "Cases", to: "/cases" },
            { label: `Case ${id}` },
          ]}
        />
      </div>

      <ActionPage actions={pageActions} buttonClass={greyButtonClass} />

      <div className="w-full max-w-4xl mb-6">
        <iframe
          src={pdfUrl}
          title="Case PDF"
          width="100%"
          height="600px"
          className="border rounded shadow"
        />
      </div>
    </div>
  );
}
