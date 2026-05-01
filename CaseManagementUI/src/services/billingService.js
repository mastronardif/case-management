// billingService.js
import { format } from "date-fns";
import api from "./http";

const MOCKAROO_KEY = "76162860"; // replace with your Mockaroo API key
const COUNT = "76162860";
const MOCKAROO_URL = `https://api.mockaroo.com/api/generate.json?count=${COUNT}&key=${MOCKAROO_KEY}`;

export async function fetchBillingData(count = 20) {
  const payload = [
    { name: "date", type: "Datetime" },
    {
      name: "invoiceNumber",
      type: "Number",
      decimals: 0,
      min: 1000,
      max: 9999,
    },
    { name: "payer", type: "Last Name" },
    { name: "case", type: "First Name" },
  ];

  try {
    const response = await api.post(`${MOCKAROO_URL}&count=${count}`, payload, {
      headers: { "Content-Type": "application/json" },
    });

    const rawData = response.data || [];
    // Normalize + format
    const formatted = rawData.map((row) => ({
      ...row,
      date: row.date ? format(new Date(row.date), "yyyy/MM/dd") : "",
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

export async function fetchBillingData22(id, count = 10) {
  const payload = [
    { name: "date", type: "Datetime" },
    { name: "invoice", type: "Number", min: 1000, max: 9999, decimals: 0 },
    { name: "service", type: "Number", min: 100, max: 999, decimals: 0 },
    { name: "code", type: "Number", min: 10, max: 99, decimals: 0 },
    {
      name: "total",
      type: "Money",
      min: 100,
      max: 300,
      options: { null_percentage: 0, currency: "USD", min: 10009, max: 310000 },
    },
    { name: "rbt", type: "Money", min: 100, max: 200 },
  ];

  try {
    const response = await api.post(`${MOCKAROO_URL}&count=${count}`, payload, {
      headers: { "Content-Type": "application/json" },
    });

    const rawData = response.data || [];
    // Normalize + format

    // Normalize + format + swap total and rbt if total < rbt
    const formatted = rawData.map((row) => {
      let total = row.total;
      let rbt = row.rbt;

      if (total < rbt) {
        [total, rbt] = [rbt, total]; // swap
      }

      return {
        ...row,
        total,
        rbt,
        date: row.date ? format(new Date(row.date), "yyyy/MM/dd") : "",
      };
    });

    return formatted;
  } catch (err) {
    console.error(
      "Error fetching billing data:",
      err.response?.data || err.message
    );
    return [];
  }
}
