export type CourseStatus = 'Active' | 'Completed' | 'Paused'

export interface Course {
  id: string
  title: string
  subject?: string | null
  status: CourseStatus
  startedAt: string
  notes?: string | null
  createdAt: string
}

export interface StudySession {
  id: string
  courseId?: string | null
  topic: string
  durationMinutes: number
  date: string
  notes?: string | null
  createdAt: string
}

export interface LearningNote {
  id: string
  title: string
  content: string
  tags: string[]
  createdAt: string
  updatedAt: string
}

export type LearningGoalStatus = 'Active' | 'Achieved' | 'Abandoned'

export interface LearningGoal {
  id: string
  description: string
  targetDate?: string | null
  status: LearningGoalStatus
  createdAt: string
}

export interface DashboardSummary {
  studyMinutesThisWeek: number
  activeCoursesCount: number
  activeGoalsCount: number
  recentSessions: StudySessionSummaryRow[]
}

export interface StudySessionSummaryRow {
  id: string
  topic: string
  durationMinutes: number
  date: string
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
