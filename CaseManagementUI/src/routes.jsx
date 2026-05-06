// routes.js
import BillingPage from "./pages/BillingPage";
import BillingTablePage from "./pages/BillingTablePage";
import CalendarPage from "./pages/CalendarPage";
import CasePage from "./pages/CasePage";
import CasesTablePage from "./pages/CasesTablePage";
import DataPage from "./pages/DataPage";
import FilesTablePage from "./pages/FilesTablePage";
import FileViewerPage from "./pages/FileViewerPage";
import IframeView from "./pages/IframeView";
import Login from "./pages/Login";
import Settings from "./pages/Settings";
import TableFromUrl from "./pages/TableFromUrl";
import WorkbooksTablePage from "./pages/WorkbooksPage";

const routes = [
  { path: "/", redirect: "/table" },

  { path: "/cases", element: <CasesTablePage />, label: "Cases" },
  { path: "/cases/new", element:  <CasePage />, hideFromNav: true },
  { path: "/cases/:caseId", element: <CasePage />, hideFromNav: true }, // dynamic route

  { path: "/workbooks/:caseId", element: <WorkbooksTablePage />, hideFromNav: true }, // dynamic route

    // ========================
  // DATA PAGE (generic)
  // ========================
  { path: "/cases/:caseId/workbooks/:workbookId/data", element: <DataPage />, hideFromNav: true },
  // { path: "/test", element: <DataPage />, label: "Test" },
   //{ path: "/data/cases", element: <DataPage />, label: "Test" },
  { path: "/data/:resource/:type?/:id?", element: <DataPage />, label: "Test", link: "/data/cases" },
  { path: "/data/:resource/:type?/:id?", element: <DataPage />, label: "Calendar", link: "/data/calendar/month/1001" },



//   /data/workbooks            → all workbooks
// /data/workbooks/case/5     → workbooks by case
// /data/cases                → search cases

  { path: "/viewer", element: <FileViewerPage />, hideFromNav: true },

  // {
  //   path: "/iframe/session/:vvv?",
  //   element: <IframeView />,
  //   label: "Session Note",
  //   defaultParams: {
  //     vvv: encodeURIComponent(
  //       "https://joeschedule.com/dist/minds/form1002.html"
  //     ),
  //   },
  // },
  { path: "/templates", element: <FilesTablePage />, label: "Templates" },
  { path: "/calendar", element: <CalendarPage />, label: "Calendar" },
  
  { path: "/billing", element: <BillingTablePage />, label: "Billing" },
  { path: "/billing/:caseId", element: <BillingPage />, hideFromNav: true }, // dynamic route

  { path: "*", element: <div>Not Found</div>, hideFromNav: true },

  { path: "/table", element: <TableFromUrl />, label: "Table" },
  { path: "/settings", element: <Settings />, label: "Settings" },
  { path: "/login", element: <Login />, label: "Login" },

  // IframeView examples
  {
    path: "/iframe/vexchords/:vvv?",
    element: <IframeView />,
    label: "Vex Chords",
    defaultParams: {
      vvv: encodeURIComponent("https://vexflow.com/vexchords/index.html"),
    },
  },
];

export default routes;
