const store = {};

export const setTableActions = (key, actions) => { store[key] = actions; };
export const getTableActions = (key) => store[key] ?? [];
export const clearTableActions = (key) => { delete store[key]; };
