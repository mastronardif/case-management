// payerdocService.js
import api from "./http";

// const baseUrl = import.meta.env.VITE_API_BASE_URL;
// const PAYERDOC_URL = `${baseUrl}/PayerDocs/for`;
// const MOCKAROO_KEY = "76162860"; // replace with your Mockaroo API key
// const COUNT = "76162860";
// const PAYERDOC_URL = `https://localhost:7009/api/PayerDocs/for`;
const PAYERDOC_URL = '/PayerDocs/for';

export async function fetchPayerDocs(caseId, sessionIds) {
  const payload = {
    caseId: Number(caseId), // convert to number just in case
    sessionIds: sessionIds.map(Number), // ensure all elements are numbers
  };

  try {
    const response = await api.post(`${PAYERDOC_URL}`, payload, {
      headers: { "Content-Type": "application/json" },
    });

    const rawData = Array.isArray(response.data) ? response.data : [];

    const formatted = rawData.map((row) => ({
      ...row,
      date: row.date,
    }));

    return formatted;
  } catch (err) {
    console.error(
      "Error fetching billing data:",
      err.response?.data || err.message
    );
    return [];
  }
}
