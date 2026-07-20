import { useState } from "react";
import { Navigate, Route, BrowserRouter as Router, Routes, useSearchParams } from "react-router-dom";
import Footer from "./components/Footer";
import Header from "./components/Header";
import Sidebar from "./components/Sidebar";
import Spinner from "./components/Spinner";
import { GlobalProvider } from "./context/GlobalContext";
import routes from "./routes";

function AppLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [searchParams] = useSearchParams();
  const embed = searchParams.get("embed") === "1";

  const routesEl = (
    <Routes>
      {routes.map((route, i) =>
        route.redirect ? (
          <Route
            key={i}
            path={route.path}
            element={<Navigate to={route.redirect} replace />}
          />
        ) : (
          <Route key={i} path={route.path} element={route.element} />
        )
      )}
    </Routes>
  );

  if (embed) {
    // Chrome-free view — append ?embed=1 to any route (e.g. for sharing a clean docviewer link)
    return (
      <div className="h-screen w-screen overflow-auto bg-gray-50 p-4" style={{ WebkitOverflowScrolling: "touch" }}>
        <Spinner />
        {routesEl}
      </div>
    );
  }

  return (
    <div className="flex h-screen w-screen relative">
      <Sidebar open={sidebarOpen} toggle={() => setSidebarOpen(!sidebarOpen)} />
      <div className="flex flex-col flex-1 overflow-hidden">
        <Header />
        <main className="flex-1 min-h-0 bg-gray-50 overflow-auto p-4 relative" style={{ WebkitOverflowScrolling: "touch" }}>
          <Spinner />
          {routesEl}
        </main>
        <Footer />
      </div>
    </div>
  );
}

export default function App() {
  return (
    <GlobalProvider>
      <Router basename={import.meta.env.BASE_URL}>
        <AppLayout />
      </Router>
    </GlobalProvider>
  );
}
