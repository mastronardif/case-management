import axios from "axios";

const api = axios.create({
  // baseURL: optional if you have a common prefix
  // timeout: 5000
});

export default api;
