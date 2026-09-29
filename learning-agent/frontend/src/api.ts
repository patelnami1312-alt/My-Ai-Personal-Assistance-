import type {
  ChatTurnResult, Course, DashboardSummary, LearningGoal, LearningNote, StudySession,
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

  courses: () => http<Course[]>('/courses'),
  createCourse: (body: Partial<Course>) =>
    http<Course>('/courses', { method: 'POST', body: JSON.stringify(body) }),
  updateCourse: (id: string, body: Partial<Course>) =>
    http<Course>(`/courses/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteCourse: (id: string) => http<void>(`/courses/${id}`, { method: 'DELETE' }),

  studySessions: () => http<StudySession[]>('/study-sessions'),
  createStudySession: (body: Partial<StudySession>) =>
    http<StudySession>('/study-sessions', { method: 'POST', body: JSON.stringify(body) }),
  deleteStudySession: (id: string) => http<void>(`/study-sessions/${id}`, { method: 'DELETE' }),

  notes: (q?: string) => http<LearningNote[]>(`/notes${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  createNote: (body: { title: string; content: string; tags?: string[] }) =>
    http<LearningNote>('/notes', { method: 'POST', body: JSON.stringify(body) }),
  updateNote: (id: string, body: { title: string; content: string; tags?: string[] }) =>
    http<LearningNote>(`/notes/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteNote: (id: string) => http<void>(`/notes/${id}`, { method: 'DELETE' }),

  goals: () => http<LearningGoal[]>('/goals'),
  createGoal: (body: Partial<LearningGoal>) =>
    http<LearningGoal>('/goals', { method: 'POST', body: JSON.stringify(body) }),
  updateGoal: (id: string, body: Partial<LearningGoal>) =>
    http<LearningGoal>(`/goals/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteGoal: (id: string) => http<void>(`/goals/${id}`, { method: 'DELETE' }),
}
