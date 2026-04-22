import api from "./http";

export async function fetchFileList(urlFiles) {
  if (!urlFiles) throw new Error("No URL provided");

  // Remote HTTP(S) request
  if (urlFiles.startsWith("http://") || urlFiles.startsWith("https://")) {
    const res = await api.get(urlFiles);
    return res.data;
  }

  // Local file (served from public/)
  if (urlFiles.startsWith("file://")) {
    const relativePath = urlFiles.replace("file://", "");
    const resp = await api.get(relativePath);
    return resp.data
    //return await resp.json();
  }

  throw new Error(`Unsupported protocol in urlFiles: ${urlFiles}`);
}
