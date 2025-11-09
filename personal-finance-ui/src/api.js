const fallbackBase =
  (typeof window !== 'undefined' && window.location?.protocol === 'https:')
    ? 'https://localhost:5001'
    : 'http://localhost:5000';

const API_BASE_RAW =
  (typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_API_BASE) || fallbackBase;

const API_BASE = String(API_BASE_RAW).replace(/\/+$/, '');

async function readError(res) {
  let txt = '';
  try { txt = await res.text(); } catch {}
  try {
    const j = txt ? JSON.parse(txt) : {};
    const msg = j.error || j.message || txt || res.statusText;
    return new Error(msg);
  } catch {
    return new Error(txt || res.statusText);
  }
}

export async function apiGet(path) {
  const r = await fetch(`${API_BASE}${path}`, {
    headers: { Accept: 'application/json' },
    credentials: 'omit'
  });
  if (!r.ok) throw await readError(r);
  return r.status === 204 ? null : r.json();
}

export async function apiPost(path, body) {
  const r = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    credentials: 'omit',
    body: body == null ? null : JSON.stringify(body)
  });
  if (!r.ok) throw await readError(r);
  return r.status === 204 ? null : r.json();
}

function normTx(t) {
  return {
    id: t.id ?? t.Id,
    date: (t.date ?? t.Date ?? '').toString(),
    amount: Number(t.amount ?? t.Amount ?? 0),
    type: t.type ?? t.Type,
    description: t.description ?? t.Description ?? ''
  };
}

export function normWallet(w) {
  const txs = w.transactions ?? w.Transactions;
  return {
    id: w.id ?? w.Id,
    name: (w.name ?? w.Name ?? '').trim(),
    currency: (w.currency ?? w.Currency ?? 'RUB').trim().toUpperCase(),
    initialBalance: Number(w.initialBalance ?? w.InitialBalance ?? 0),
    currentBalance: w.currentBalance ?? w.CurrentBalance,
    transactions: Array.isArray(txs) ? txs.map(normTx) : []
  };
}

function extractWallets(res) {
  if (Array.isArray(res)) return res;
  if (res && Array.isArray(res.wallets)) return res.wallets;
  if (res && Array.isArray(res.Wallets)) return res.Wallets;
  return [];
}

export const api = {
  getWallets: () =>
    apiGet('/api/wallets').then(res => extractWallets(res).map(normWallet)),

  createWallet: (w) =>
    apiPost('/api/wallets', {
      name: String(w.name ?? '').trim(),
      currency: String(w.currency ?? 'RUB').trim(),
      initialBalance: Number(w.initialBalance ?? 0)
    }).then(normWallet),

  addTx: (walletId, tx) =>
    apiPost(`/api/wallets/${walletId}/transactions`, {
      date: tx.date,
      amount: Number(tx.amount ?? 0),
      type: tx.type === 'Income' ? 'Income' : 'Expense',
      description: String(tx.description ?? '').trim()
    }).then(normTx),

  generateSample: async () => {
    const res = await apiPost('/api/sample', null);
    const list = Array.isArray(res) ? res : (Array.isArray(res?.wallets) ? res.wallets : []);
    return list.map(normWallet);
  },

  getReport: (y, m, currency) =>
    apiGet(`/api/report?year=${y}&month=${m}${currency ? `&currency=${encodeURIComponent(currency)}` : ''}`)
};

export { API_BASE };
