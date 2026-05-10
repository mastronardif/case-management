import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../services/http";

export default function DocumentViewerPage() {
  const { documentId } = useParams();
  const navigate = useNavigate();
  const [blobUrl, setBlobUrl] = useState(null);
  const [mimeType, setMimeType] = useState(null);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!documentId) return;

    let objectUrl;

    const fetchDoc = async () => {
      setLoading(true);
      try {
        const res = await api.get(`/api/corqs/getDocument`, {
          params: { documentId },
          responseType: "blob",
        });

        const contentType = res.headers["content-type"] ?? "application/octet-stream";
        const blob = new Blob([res.data], { type: contentType });
        objectUrl = URL.createObjectURL(blob);
        setMimeType(contentType.split(";")[0].trim());
        setBlobUrl(objectUrl);
      } catch (err) {
        setError("Failed to load document.");
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    fetchDoc();

    return () => {
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [documentId]);

  const renderContent = () => {
    if (loading) return <p className="text-gray-500">Loading...</p>;
    if (error)   return <p className="text-red-500">{error}</p>;
    if (!blobUrl) return null;

    if (mimeType?.startsWith("text/")) {
      return (
        <iframe
          src={blobUrl}
          title="Document"
          className="w-full h-full border-0"
        />
      );
    }

    if (mimeType?.startsWith("image/")) {
      return (
        <img
          src={blobUrl}
          alt="Document"
          className="max-h-full max-w-full object-contain"
        />
      );
    }

    // PDF, HTML, everything else
    return (
      <iframe
        src={blobUrl}
        title="Document"
        className="w-full h-full border-0"
      />
    );
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-3 px-4 py-2 bg-gray-100 border-b flex-shrink-0">
        <button
          onClick={() => navigate(-1)}
          className="px-3 py-1 text-sm rounded border border-gray-300 bg-white hover:bg-gray-50"
        >
          ← Back
        </button>
        <span className="text-sm text-gray-600 font-mono">{documentId}</span>
        {mimeType && <span className="text-xs text-gray-400">{mimeType}</span>}
      </div>

      <div className="flex-1 overflow-hidden flex items-center justify-center bg-gray-50">
        {renderContent()}
      </div>
    </div>
  );
}
