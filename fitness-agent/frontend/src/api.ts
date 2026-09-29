import type {
  BodyWeightEntry, ChatTurnResult, DashboardSummary, FitnessGoal, NutritionEntry, WorkoutEntry,
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

  workouts: () => http<WorkoutEntry[]>('/workouts'),
  createWorkout: (body: Partial<WorkoutEntry>) =>
    http<WorkoutEntry>('/workouts', { method: 'POST', body: JSON.stringify(body) }),
  deleteWorkout: (id: string) => http<void>(`/workouts/${id}`, { method: 'DELETE' }),

  bodyWeights: () => http<BodyWeightEntry[]>('/bodyweight'),
  createBodyWeight: (body: Partial<BodyWeightEntry>) =>
    http<BodyWeightEntry>('/bodyweight', { method: 'POST', body: JSON.stringify(body) }),
  deleteBodyWeight: (id: string) => http<void>(`/bodyweight/${id}`, { method: 'DELETE' }),

  meals: () => http<NutritionEntry[]>('/nutrition'),
  createMeal: (body: Partial<NutritionEntry>) =>
    http<NutritionEntry>('/nutrition', { method: 'POST', body: JSON.stringify(body) }),
  deleteMeal: (id: string) => http<void>(`/nutrition/${id}`, { method: 'DELETE' }),

  goals: () => http<FitnessGoal[]>('/goals'),
  createGoal: (body: Partial<FitnessGoal>) =>
    http<FitnessGoal>('/goals', { method: 'POST', body: JSON.stringify(body) }),
  updateGoal: (id: string, body: Partial<FitnessGoal>) =>
    http<FitnessGoal>(`/goals/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteGoal: (id: string) => http<void>(`/goals/${id}`, { method: 'DELETE' }),
}
