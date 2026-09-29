import type {
  BriefingTask, CalendarEvent, ChatTurnResult, ConversationDetail,
  ConversationSummary, DailyBriefing, MemoryItem, Note, TaskItem, UserProfile,
} from './types'

const BASE = '/api'

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(BASE + path, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(`${res.status} ${res.statusText}${text ? ` — ${text}` : ''}`)
  }
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const api = {
  health: () => http<{ status: string; time: string }>('/health'),

  // Chat
  sendMessage: (message: string, conversationId?: string) =>
    http<ChatTurnResult>('/chat', {
      method: 'POST',
      body: JSON.stringify({ message, conversationId: conversationId ?? null }),
    }),
  conversations: () => http<ConversationSummary[]>('/conversations'),
  conversation: (id: string) => http<ConversationDetail>(`/conversations/${id}`),
  deleteConversation: (id: string) => http<void>(`/conversations/${id}`, { method: 'DELETE' }),

  // Briefing
  briefing: () => http<DailyBriefing>('/briefing'),

  // Tasks
  tasks: (status?: string) => http<TaskItem[]>(`/tasks${status ? `?status=${status}` : ''}`),
  createTask: (body: Partial<TaskItem>) => http<TaskItem>('/tasks', { method: 'POST', body: JSON.stringify(body) }),
  updateTask: (id: string, body: Partial<TaskItem>) =>
    http<TaskItem>(`/tasks/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteTask: (id: string) => http<void>(`/tasks/${id}`, { method: 'DELETE' }),

  // Notes
  notes: (q?: string) => http<Note[]>(`/notes${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  createNote: (body: Partial<Note>) => http<Note>('/notes', { method: 'POST', body: JSON.stringify(body) }),
  updateNote: (id: string, body: Partial<Note>) =>
    http<Note>(`/notes/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteNote: (id: string) => http<void>(`/notes/${id}`, { method: 'DELETE' }),

  // Calendar
  events: (from?: string, to?: string) => {
    const p = new URLSearchParams()
    if (from) p.set('from', from)
    if (to) p.set('to', to)
    return http<CalendarEvent[]>(`/events${p.toString() ? `?${p}` : ''}`)
  },
  createEvent: (body: Partial<CalendarEvent>) =>
    http<CalendarEvent>('/events', { method: 'POST', body: JSON.stringify(body) }),
  updateEvent: (id: string, body: Partial<CalendarEvent>) =>
    http<CalendarEvent>(`/events/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteEvent: (id: string) => http<void>(`/events/${id}`, { method: 'DELETE' }),

  // Memory
  memories: (q?: string) => http<MemoryItem[]>(`/memories${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  createMemory: (body: { content: string; category?: string; keywords?: string[] }) =>
    http<MemoryItem>('/memories', { method: 'POST', body: JSON.stringify(body) }),
  deleteMemory: (id: string) => http<void>(`/memories/${id}`, { method: 'DELETE' }),

  // Profile
  profile: () => http<UserProfile>('/profile'),
  updateProfile: (body: Partial<UserProfile>) =>
    http<UserProfile>('/profile', { method: 'PUT', body: JSON.stringify(body) }),
}

export type { BriefingTask }
