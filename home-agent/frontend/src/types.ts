export type ChoreStatus = 'Todo' | 'Done'
export type RecurrenceFrequency = 'OneTime' | 'Daily' | 'Weekly' | 'Monthly'

export interface Chore {
  id: string
  title: string
  assignedTo?: string | null
  dueDate?: string | null
  recurring: boolean
  frequency: RecurrenceFrequency
  status: ChoreStatus
  notes?: string | null
  createdAt: string
  completedAt?: string | null
}

export interface MaintenanceItem {
  id: string
  title: string
  lastDone?: string | null
  intervalDays?: number | null
  nextDue?: string | null
  notes?: string | null
  createdAt: string
}

export interface InventoryItem {
  id: string
  name: string
  category?: string | null
  quantity: number
  lowThreshold: number
  notes?: string | null
  createdAt: string
  updatedAt: string
}

export interface UpcomingMaintenanceRow {
  id: string
  title: string
  nextDue?: string | null
}

export interface RecentChoreRow {
  id: string
  title: string
  status: ChoreStatus
  dueDate?: string | null
}

export interface DashboardSummary {
  choresDueTodayCount: number
  overdueChoresCount: number
  lowInventoryCount: number
  upcomingMaintenance: UpcomingMaintenanceRow[]
  recentChores: RecentChoreRow[]
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
