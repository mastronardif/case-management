import { useState } from "react";
import { Navigate, Route, BrowserRouter as Router, Routes } from "react-router-dom";
import Footer from "./components/Footer";
import Header from "./components/Header";
import Sidebar from "./components/Sidebar";
import Spinner from "./components/Spinner";
import { GlobalProvider } from "./context/GlobalContext";
import routes from "./routes";

export default function App() {
  const [sidebarOpen, setSidebarOpen] = useState(true);

  return (
    <GlobalProvider>
  
      <Router basename={import.meta.env.BASE_URL}>
        <div className="flex h-screen w-screen relative">
          <Sidebar open={sidebarOpen} toggle={() => setSidebarOpen(!sidebarOpen)} />
          <div className="flex flex-col flex-1 overflow-hidden">
            <Header />
            <main className="flex-1 min-h-0 bg-gray-50 overflow-auto p-4 relative" style={{ WebkitOverflowScrolling: "touch" }}>        
                           <Spinner /> 
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
            </main>
            <Footer />
          </div>
        </div>
      </Router>
    </GlobalProvider>
  );
}
