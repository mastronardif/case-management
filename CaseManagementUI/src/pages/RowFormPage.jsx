import { useLocation, useNavigate } from "react-router-dom";
import { renderValue } from "../components/DataTable";

const isPlainObject = (value) => value && typeof value === "object" && !value.$$typeof;

export default function RowFormPage() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const title = state?.title;
  const row = state?.row;

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-3 px-4 py-2 bg-gray-100 border-b flex-shrink-0">
        <button
          onClick={() => navigate(-1)}
          className="px-3 py-1 text-sm rounded border border-gray-300 bg-white hover:bg-gray-50"
        >
          ← Back
        </button>
        <span className="text-sm text-gray-600 font-semibold">{title || "Form"}</span>
      </div>

      <div className="flex-1 overflow-auto bg-gray-50 p-6 flex justify-center">
        {!isPlainObject(row) ? (
          <p className="text-gray-500 mt-4">
            Nothing to show — this page only works when opened from its "Form" button; the data
            isn't reloaded on a direct visit or a refresh.
          </p>
        ) : (
          <div className="w-full max-w-2xl bg-white rounded shadow border border-gray-200 h-fit">
            <table className="w-full border-collapse">
              <tbody>
                {Object.entries(row).map(([key, value], i) => (
                  <tr key={key} className={i % 2 === 0 ? "bg-white" : "bg-gray-50"}>
                    <td className="border border-gray-200 px-3 py-2 font-medium text-gray-600 w-1/3 align-top">
                      {key}
                    </td>
                    <td className="border border-gray-200 px-3 py-2 text-gray-900 break-words">
                      {renderValue(value)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
