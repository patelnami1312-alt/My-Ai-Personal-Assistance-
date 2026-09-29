import type {
  ChatTurnResult, Chore, DashboardSummary, InventoryItem, MaintenanceItem,
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

  chores: () => http<Chore[]>('/chores'),
  createChore: (body: Partial<Chore>) =>
    http<Chore>('/chores', { method: 'POST', body: JSON.stringify(body) }),
  updateChore: (id: string, body: Partial<Chore>) =>
    http<Chore>(`/chores/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteChore: (id: string) => http<void>(`/chores/${id}`, { method: 'DELETE' }),

  maintenance: () => http<MaintenanceItem[]>('/maintenance'),
  createMaintenance: (body: Partial<MaintenanceItem>) =>
    http<MaintenanceItem>('/maintenance', { method: 'POST', body: JSON.stringify(body) }),
  markMaintenanceDone: (id: string) =>
    http<MaintenanceItem>(`/maintenance/${id}/mark-done`, { method: 'PUT' }),
  deleteMaintenance: (id: string) => http<void>(`/maintenance/${id}`, { method: 'DELETE' }),

  inventory: () => http<InventoryItem[]>('/inventory'),
  createInventoryItem: (body: Partial<InventoryItem>) =>
    http<InventoryItem>('/inventory', { method: 'POST', body: JSON.stringify(body) }),
  updateInventoryItem: (id: string, body: Partial<InventoryItem>) =>
    http<InventoryItem>(`/inventory/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteInventoryItem: (id: string) => http<void>(`/inventory/${id}`, { method: 'DELETE' }),
}
