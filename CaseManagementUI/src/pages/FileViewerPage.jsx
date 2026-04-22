import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";

export default function FileViewerPage() {
  const location = useLocation();
  const { fileUrl: rawFileUrl, title } = location.state || {};

// Normalize fileUrl to a string
const fileUrl = typeof rawFileUrl === "string"
  ? rawFileUrl
  : rawFileUrl?.fileUrl ?? "";


  const lower = fileUrl.toLowerCase();
//   const [textContent, setTextContent] = useState<string | null>(null);
  const [textContent, setTextContent] = useState(null);


  useEffect(() => {
    const loadText = async () => {
      // Only fetch text for .txt files AND local files
      if (lower.endsWith(".txt") && !fileUrl.startsWith("http")) {
        try {
          const res = await fetch(fileUrl);
          const text = await res.text();
          setTextContent(text);
        } catch (err) {
          console.error("Error loading text file:", err);
          setTextContent("Failed to load text file.");
        }
      } else {
        setTextContent(null); // clear textContent for remote or non-txt
      }
    };
    loadText();
  }, [fileUrl, lower]);

  if (!fileUrl) {
    return <p className="p-6 text-red-500">No file to display</p>;
  }

  const isImage = /\.(png|jpe?g|gif|webp|svg)$/i.test(lower);

  return (
    <div className="relative min-h-screen p-6 flex flex-col items-center">
      {title && <h1 className="text-xl font-bold mb-4">{title}</h1>}

      {/* Text file */}
      {textContent !== null ? (
        <div className="w-full max-w-6xl h-[600px] border rounded shadow overflow-auto bg-gray-50 p-4">
          <pre className="whitespace-pre-wrap">{textContent}</pre>
        </div>
      ) : isImage ? (
        // Image
        <div className="w-full max-w-6xl h-[600px] border rounded shadow flex items-center justify-center bg-gray-50">
          <img src={fileUrl} alt="Preview" className="max-h-full max-w-full object-contain" />
        </div>
      ) : (
        // PDF, HTML, remote or local
        <div className="w-full max-w-6xl h-[600px] border rounded shadow">
          <iframe src={fileUrl} title="File Viewer" className="w-full h-full" />
        </div>
      )}
    </div>
  );
}
