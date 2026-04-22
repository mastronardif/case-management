// src/pages/IframeView.jsx
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useGlobalStore } from "../context/GlobalStore";

export default function IframeView() {
  const { vvv } = useParams();
  const [src, setSrc] = useState("");
  const { setLoading } = useGlobalStore();

  useEffect(() => {
    if (vvv) {
      setSrc(decodeURIComponent(vvv));
      setLoading(true); // global spinner ON when url changes
    }
  }, [vvv, setLoading]);

  if (!src) {
    return <p className="text-red-500">No URL provided.</p>;
  }

  return (
    <div className="relative w-full h-[80vh] border rounded-lg overflow-hidden">
      <iframe
        src={src}
        className="w-full h-full border-0"
        onLoad={() => setLoading(false)} // global spinner OFF
        title="Embedded Page"
      />
    </div>
  );
}
