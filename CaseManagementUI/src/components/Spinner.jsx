import { useGlobalStore } from "../context/GlobalStore";

export default function Spinner() {
  const { loading } = useGlobalStore();

  if (!loading) return null;

  return (
    <div className="absolute inset-0 flex items-center justify-center bg-[rgba(255,255,255,0.6)] backdrop-blur-sm z-50">
      <div className="w-16 h-16 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
    </div>
  );
}
