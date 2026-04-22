import { Link } from "react-router-dom";

export default function Navbar() {
  return (
    <header className="flex items-center justify-between bg-white shadow px-4 py-2">
      <h1 className="text-xl font-bold">Case Management 123</h1>
      <nav className="space-x-4">
        <Link className="text-gray-700 hover:text-gray-900" to="/">
          Home
        </Link>
        <Link className="text-gray-700 hover:text-gray-900" to="/settings">
          Settings
        </Link>
        <Link className="text-gray-700 hover:text-gray-900" to="/login">
          login
        </Link>
      </nav>
    </header>
  );
}
