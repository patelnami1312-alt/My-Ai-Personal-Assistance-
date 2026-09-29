import type { ChatTurnResult, DashboardSummary, ShoppingItem } from './types'

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

  items: (purchased?: boolean) =>
    http<ShoppingItem[]>(`/items${purchased === undefined ? '' : `?purchased=${purchased}`}`),
  createItem: (body: Partial<ShoppingItem>) =>
    http<ShoppingItem>('/items', { method: 'POST', body: JSON.stringify(body) }),
  updateItem: (id: string, body: Partial<ShoppingItem>) =>
    http<ShoppingItem>(`/items/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteItem: (id: string) => http<void>(`/items/${id}`, { method: 'DELETE' }),
}
