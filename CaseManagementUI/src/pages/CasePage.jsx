// CasePage.jsx
import { useNavigate, useParams } from "react-router-dom";
import ActionPage from "../components/ActionPage";

export default function CasePage() {
  //const [pdfUrl] = useState("/files/caseIntake.pdf"); // Update with your PDF path
    // Always resolve to /dist/myreactapp/files/...
  // const pdfUrl = `${process.env.PUBLIC_URL}/files/caseIntake.pdf`;
  const navigate = useNavigate();
  const pdfUrl = `${import.meta.env.BASE_URL}files/intake.html`;

    // Grey button style for all buttons
  const greyButtonClass =
    "flex items-center justify-center px-4 py-1 h-9 text-sm rounded-md border border-gray-300 bg-gray-100 text-gray-700 hover:bg-gray-200 transition-colors duration-150 flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed";

    const pageActions = [
    { label: "Work Books", onClick: () => handleWorkbooks() },
    { label: "RBT Books", onClick: () => handleRBTbooks() },
    { label: "Insurance Books", onClick: () => handleInsurancebooks() },
    { label: "Import from Scan", onClick: () => handleImportScan() },
    { label: "tbd", onClick: () => handleFillForm() },
  ];

  const { caseId } = useParams(); // URL param
  const id = caseId ?? "new"; // ✅ use nullish coalescing

  const handleImportScan = () => {
    alert("Import from scan feature coming soon!");
    // future implementation: open file picker or scanner integration
  };

  const handleWorkbooks = (row) => {
    alert(`Work Book feature coming soon! 
      0. Get OK from Insurance 88
      1. Assessment 
      2. Treatment plan 
      3. Progress report`);
    // future implementation: navigate to form component/page
    // navigate(`/workbooks/${id}`, { state: { caseData: row } });

     // navigate(`/data/calendar/month/${id}`, { state: { caseData: row } });
    //  navigate(`/data/getWorkbooksByCase/caseId/${id}`, { state: { caseData: row } });
    //  navigate(`/data/getBook`, { state: { caseId: row.caseId, fileName: row.fileName } });
    console.log("Navigating to data page with caseId:", row.caseId);
     navigate(`/data/getBook`, { state: { caseId: "CASE-2026-000002", fileName: "session.187.aba_session_editable.pdf" } });

    //  { path: "/data/:resource/:type?/:id?", element: <DataPage />, label: "Test", link: "/data/cases" },
    //   { path: "/data/:resource/:type?/:id?", element: <DataPage />, label: "Calendar", link: "/data/calendar/month/1001" },
    
    

  };
  const handleRBTbooks = (row) => {
      alert(`RBT Work Book feature coming soon!
      1. Check docs 77
      2. Check hours
      3. Verify hours
      4 *Audit
      5. Payroll`); 
       navigate(`/workbooks/${id}`, { state: { caseData: row } });
    // future implementation: navigate to form component/page
  };
    const handleInsurancebooks = (row) => {
    alert(`Insurance Book feature coming soon! 
      1. Create docs for insurance.
      2. Report status 
      3. Update invoice status`);
    // future implementation: navigate to form component/page
    navigate(`/table`, { state: { caseData: row } });
    // navigate(`/workbooks/${id}`, { state: { caseData: row } });
  };

  const handleFillForm = () => {
    alert("Fill out form feature coming soon!");
    // navigate(`/templates`, { state: { caseData: 888 } });
    // future implementation: navigate to form component/page
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-start p-6 bg-gray-50">
      <h1 className="text-3xl font-bold mb-6">Case {id}</h1>

      {/* ✅ Shared header actions */}
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
