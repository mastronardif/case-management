let status = "connected";
let listeners = [];

export const setConnected = (v) => {
  status = v;
  listeners.forEach((fn) => fn(v));
};

export const subscribeConnection = (fn) => {
  listeners.push(fn);
  return () => { listeners = listeners.filter((l) => l !== fn); };
};

export const getConnectionStatus = () => status;
