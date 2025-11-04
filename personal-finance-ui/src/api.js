// Базовый URL: из .env (Vite) или дефолт
const API_BASE =
  (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_API_BASE) ||
  'http://localhost:5001';

// Универсальная подстраховка текста ошибки
async function safeText(r) {
  try { return await r.text(); } catch { return `${r.status} ${r.statusText}`; }
}

// Базовые обёртки
export async function apiGet(path) {
  const r = await fetch(`${API_BASE}${path}`, {
    headers: { Accept: 'application/json' },
  });
  if (!r.ok) throw new Error(await safeText(r));
  return r.json();
}

export async function apiPost(path, body) {
  const r = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: body != null ? JSON.stringify(body) : null,
  });
  if (!r.ok) throw new Error(await safeText(r));
  return r.json();
}

// ===== Нормализация =====
function normTx(t) {
  return {
    id: t.id ?? t.Id,
    date: (t.date ?? t.Date ?? '').toString(),
    amount: Number(t.amount ?? t.Amount ?? 0),
    type: t.type ?? t.Type, // 'Income' | 'Expense'
    description: t.description ?? t.Description ?? ''
  };
}

export function normWallet(w) {
  const txs = w.transactions ?? w.Transactions;
  return {
    id: w.id ?? w.Id,
    name: w.name ?? w.Name ?? '',
    currency: w.currency ?? w.Currency ?? 'RUB',
    initialBalance: Number(w.initialBalance ?? w.InitialBalance ?? 0),
    transactions: Array.isArray(txs) ? txs.map(normTx) : []
  };
}

// ===== Готовые вызовы =====
export const api = {
  getWallets: () =>
    apiGet('/api/wallets').then(arr => (Array.isArray(arr) ? arr.map(normWallet) : [])),
  setWallets: (list) => apiPost('/api/wallets', list), // импорт JSON
  createWallet: (w) => apiPost('/api/wallets/create', w).then(normWallet),
  addTx: (walletId, tx) => apiPost(`/api/wallets/${walletId}/transactions`, tx).then(normTx),
  generateSample: () => apiPost('/api/sample', null),
  getReport: (y, m, currency) =>
    apiGet(`/api/report?year=${y}&month=${m}${currency ? `&currency=${encodeURIComponent(currency)}` : ''}`)
};

export { API_BASE };
