// src/config.ts

// ✅ Centralized configuration helper for environment-based settings
export const getEnv = () => import.meta.env.MODE; // "development" | "production" | "test"

export const getApiBaseUrl = (): string => {
  // Read the base URL from the environment
  const base = import.meta.env.VITE_API_BASE_URL;

  if (!base) {
    // Provide a safe fallback to localhost if not defined
    console.warn("⚠️ VITE_API_BASE_URL is not set. Using default localhost.");
    return "https://localhost:7009/api";
  }

  return base;
};

// ✅ Helper to construct full API URLs
export const getApiUrl = (endpoint: string): string => {
  const base = getApiBaseUrl();
  return `${base.replace(/\/$/, "")}/${endpoint.replace(/^\//, "")}`;
};
