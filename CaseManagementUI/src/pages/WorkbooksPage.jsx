// WorkbooksTablePage.jsx
import { useNavigate, useParams } from "react-router-dom";
import ActionRow from "../components/ActionRow";
// import { useGlobalStore } from "../context/GlobalStore";
import { fetchPayerDocs } from "../services/payerdocService";
import XyzTablePage from "./XyzTablePage";

export default function WorkbooksTablePage() {
  const navigate = useNavigate();
  const { caseId } = useParams(); // URL param
  const urlCases = `/api/Workbooks/GetBooks?caseId=${caseId}`;
  const workbookUrl = 'api/Workbooks/GetBook';

  const rowActions = [
    // {
    //   label: "Open",
    //   onClick: (row) => navigate(`/cases/${row.id}`, { state: { caseData: row } }),
    //   className: "bg-green-500 text-white hover:bg-green-600",
    // },
    {
      label: "Open",
      onClick: (row) => {
        const url = `${workbookUrl}?caseId=${encodeURIComponent(
          caseId
        )}&fileName=${encodeURIComponent(row.fileName)}`;
        navigate("/viewer", {
          state: { fileUrl: url, title: `Viewing ${row.fileName || row.id}` },
        });
      },
      className: "bg-green-500 text-white hover:bg-green-600",
    },
    {
      label: "Delete",
      onClick: (row) => alert(`Delete case ${row.id} coming soon`),
      className: "bg-red-500 text-white hover:bg-red-600",
    },
  ];

  // 🔹 Table-level actions array
  const tableActions = [
    {
      label: "Payer Docs",
      onClick: async () => {
        try {
          // Call your service with the current case + sessions
          const docs = await fetchPayerDocs(caseId, [111,222]);

          if (docs.length > 0 && docs[0].url) {
            navigate("/viewer", {
              state: {
                fileUrl: docs[0].url,
                title: `Case: ${caseId } Sessions: [111,222]`,
              },
            });
          } else {
            alert("No payer docs available for this case.");
          }
        } catch (err) {
          console.error("Error loading payer docs:", err);
          alert("Failed to fetch payer docs.");
        }
      },
      className: "bg-blue-500 text-white hover:bg-blue-600",
    },
    {
      label: "F 2",
      onClick: () => navigate(`/cases/${caseId}/new-workbook`),
      className: "bg-green-500 text-white hover:bg-green-600",
    },
  ];

  return (
    <XyzTablePage
      title={`Books for Case: ${caseId}`}
      fetchUrl={urlCases} // or your urlCases from context
      tableActions={tableActions} // <-- pass the array
      ActionRowComponent={ActionRow}
      rowActions={rowActions}
    />
  );
}
