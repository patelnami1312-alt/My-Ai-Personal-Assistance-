export type TaskState = 'Todo' | 'InProgress' | 'Done'
export type TaskPriority = 'Low' | 'Medium' | 'High'
export type MemoryCategory = 'Fact' | 'Preference' | 'Goal' | 'Relationship' | 'Event'

export interface TaskItem {
  id: string
  title: string
  description?: string | null
  priority: TaskPriority
  dueAt?: string | null
  status: TaskState
  createdAt: string
  updatedAt: string
  completedAt?: string | null
}

export interface Note {
  id: string
  title: string
  content: string
  tags: string[]
  createdAt: string
  updatedAt: string
}

export interface CalendarEvent {
  id: string
  title: string
  description?: string | null
  location?: string | null
  startsAt: string
  endsAt: string
  allDay: boolean
}

export interface MemoryItem {
  id: string
  content: string
  category: MemoryCategory
  keywords: string[]
  confidence: number
  createdAt: string
  lastUsedAt: string
}

export interface UserProfile {
  id: string
  name: string
  timeZone: string
  language: string
  preferences: string[]
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

export interface ConversationSummary {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  messageCount: number
}

export interface ChatMessageDto {
  id: string
  role: 'User' | 'Assistant' | 'System' | 'Tool'
  content: string
  toolTrace?: string | null
  createdAt: string
}

export interface ConversationDetail extends ConversationSummary {
  messages: ChatMessageDto[]
}

export interface BriefingEvent {
  id: string
  title: string
  startsAt: string
  endsAt: string
  location?: string | null
  allDay: boolean
}

export interface BriefingTask {
  id: string
  title: string
  priority: TaskPriority
  dueAt?: string | null
  status: TaskState
}

export interface DailyBriefing {
  greeting: string
  date: string
  summary: string
  events: BriefingEvent[]
  tasks: BriefingTask[]
  openTaskCount: number
  overdueTaskCount: number
}
