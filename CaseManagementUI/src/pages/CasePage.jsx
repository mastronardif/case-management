// CasePage.jsx
import { useLocation, useNavigate, useParams } from "react-router-dom";
import ActionPage from "../components/ActionPage";
import { fetchPayerDocs } from "../services/payerdocService";
import { setTableActions } from "../utils/tableActionStore";

export default function CasePage() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const caseData = state?.caseData;
  const pdfUrl = `${import.meta.env.BASE_URL}files/intake.html`;

  const greyButtonClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed";
  const blueButtonClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-blue-400 bg-blue-500 text-white hover:bg-blue-600 flex-shrink-0";
  const greenButtonClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-green-400 bg-green-500 text-white hover:bg-green-600 flex-shrink-0";

  const { caseId } = useParams();
  const id = caseId ?? "new";

  const pageActions = [
    { label: "Work Books", onClick: () => handleWorkbooks() },
    { label: "RBT Books", onClick: () => handleRBTbooks() },
    { label: "Insurance Books", onClick: () => handleInsurancebooks() },
    { label: "Import from Scan", onClick: () => handleImportScan() },
    { label: "tbd", onClick: () => handleFillForm() },
  ];

  const handleWorkbooks = () => {
    setTableActions("getWorkbooksByCase", [
      {
        label: "Payer Docs",
        onClick: async () => {
          try {
            const docs = await fetchPayerDocs(id, []);
            if (docs.length > 0 && docs[0].url)
              navigate("/viewer", { state: { fileUrl: docs[0].url, title: `Payer Docs: ${id}` } });
            else alert("No payer docs available.");
          } catch { alert("Failed to fetch payer docs."); }
        },
        className: blueButtonClass,
      },
      {
        label: "F 2",
        onClick: () => navigate(`/cases/${id}/new-workbook`),
        className: greenButtonClass,
      },
    ]);
    //navigate(`/data/getWorkbooksByCase/caseId/${id}`, { state: { caseData } });
    //navigate(`/data/getBooks22/caseId/${id}`, { state: { caseData, workbookQId: "1001" } });
    navigate(`/data/Case_GetDocuments/caseId/${id}`, { state: { caseData } });
    // navigate(`/data/getBooks22/caseId/${id}`)
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
    alert("Fill out form feature coming soon!");
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-start p-6 bg-gray-50">
      <h1 className="text-3xl font-bold mb-6">Case {id}</h1>

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
