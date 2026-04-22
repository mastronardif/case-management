import { createContext, useContext } from "react";

export const GlobalContext = createContext(null);

export function useGlobalStore() {
  const ctx = useContext(GlobalContext);
  if (!ctx) {
    throw new Error("useGlobalStore must be used inside GlobalProvider");
  }
  return ctx;
}
