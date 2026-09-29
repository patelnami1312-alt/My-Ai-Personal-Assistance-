import type {
  ChatTurnResult, DashboardSummary, ItineraryItem, PackingItem, Trip,
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

  trips: () => http<Trip[]>('/trips'),
  trip: (id: string) => http<Trip>(`/trips/${id}`),
  createTrip: (body: Partial<Trip>) =>
    http<Trip>('/trips', { method: 'POST', body: JSON.stringify(body) }),
  updateTrip: (id: string, body: Partial<Trip>) =>
    http<Trip>(`/trips/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteTrip: (id: string) => http<void>(`/trips/${id}`, { method: 'DELETE' }),

  itinerary: (tripId: string) => http<ItineraryItem[]>(`/itinerary?tripId=${tripId}`),
  createItineraryItem: (body: Partial<ItineraryItem>) =>
    http<ItineraryItem>('/itinerary', { method: 'POST', body: JSON.stringify(body) }),
  deleteItineraryItem: (id: string) => http<void>(`/itinerary/${id}`, { method: 'DELETE' }),

  packingList: (tripId: string) => http<PackingItem[]>(`/packing?tripId=${tripId}`),
  createPackingItem: (body: Partial<PackingItem>) =>
    http<PackingItem>('/packing', { method: 'POST', body: JSON.stringify(body) }),
  updatePackingItem: (id: string, body: Partial<PackingItem>) =>
    http<PackingItem>(`/packing/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deletePackingItem: (id: string) => http<void>(`/packing/${id}`, { method: 'DELETE' }),
}
