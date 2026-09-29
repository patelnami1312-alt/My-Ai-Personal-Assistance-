import type {
  Bill, Budget, ChatTurnResult, DashboardSummary, FinanceAccount, Transaction,
} from './types'

const BASE = '/api'

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(BASE + path, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(`${res.status} ${res.statusText}${text ? ` - ${text}` : ''}`)
  }
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const api = {
  health: () => http<{ status: string; time: string }>('/health'),

  sendMessage: (message: string, conversationId?: string) =>
    http<ChatTurnResult>('/chat', {
      method: 'POST',
      body: JSON.stringify({ message, conversationId: conversationId ?? null }),
    }),

  dashboard: () => http<DashboardSummary>('/dashboard'),

  transactions: () => http<Transaction[]>('/transactions'),
  createTransaction: (body: Partial<Transaction>) =>
    http<Transaction>('/transactions', { method: 'POST', body: JSON.stringify(body) }),
  deleteTransaction: (id: string) => http<void>(`/transactions/${id}`, { method: 'DELETE' }),

  accounts: () => http<FinanceAccount[]>('/accounts'),
  createAccount: (body: Partial<FinanceAccount>) =>
    http<FinanceAccount>('/accounts', { method: 'POST', body: JSON.stringify(body) }),
  updateAccount: (id: string, body: Partial<FinanceAccount>) =>
    http<FinanceAccount>(`/accounts/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteAccount: (id: string) => http<void>(`/accounts/${id}`, { method: 'DELETE' }),

  budgets: () => http<Budget[]>('/budgets'),
  createBudget: (body: Partial<Budget>) =>
    http<Budget>('/budgets', { method: 'POST', body: JSON.stringify(body) }),
  deleteBudget: (id: string) => http<void>(`/budgets/${id}`, { method: 'DELETE' }),

  bills: () => http<Bill[]>('/bills'),
  createBill: (body: Partial<Bill>) =>
    http<Bill>('/bills', { method: 'POST', body: JSON.stringify(body) }),
  updateBill: (id: string, body: Partial<Bill>) =>
    http<Bill>(`/bills/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteBill: (id: string) => http<void>(`/bills/${id}`, { method: 'DELETE' }),
}
