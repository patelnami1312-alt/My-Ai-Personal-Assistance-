export interface WorkoutEntry {
  id: string
  date: string
  exercise: string
  sets?: number | null
  reps?: number | null
  weightKg?: number | null
  durationMinutes?: number | null
  notes?: string | null
  createdAt: string
}

export interface BodyWeightEntry {
  id: string
  date: string
  weightKg: number
  notes?: string | null
  createdAt: string
}

export interface NutritionEntry {
  id: string
  date: string
  description: string
  calories?: number | null
  proteinG?: number | null
  carbsG?: number | null
  fatG?: number | null
  createdAt: string
}

export type GoalType = 'WeightTarget' | 'WorkoutFrequency' | 'Custom'
export type GoalStatus = 'Active' | 'Achieved' | 'Abandoned'

export interface FitnessGoal {
  id: string
  type: GoalType
  description: string
  targetValue?: number | null
  targetDate?: string | null
  status: GoalStatus
  createdAt: string
}

export interface DashboardSummary {
  workoutsThisWeek: number
  latestWeightKg?: number | null
  weightChange30dKg?: number | null
  activeGoals: number
  recentWorkouts: WorkoutSummaryRow[]
}

export interface WorkoutSummaryRow {
  id: string
  date: string
  exercise: string
  sets?: number | null
  reps?: number | null
  weightKg?: number | null
}

export interface ToolTraceEntry {
  tool: string
  arguments: string
  result: string
  isError: boolean
}

export interface ChatTurnResult {
  conversationId: string
  reply: string
  toolTrace: ToolTraceEntry[]
  inputTokens: number
  outputTokens: number
}
