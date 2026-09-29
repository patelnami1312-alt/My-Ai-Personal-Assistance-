export interface ShoppingItem {
  id: string
  name: string
  quantity: number
  category?: string | null
  store?: string | null
  estimatedPrice?: number | null
  purchased: boolean
  notes?: string | null
  createdAt: string
  purchasedAt?: string | null
}

export interface DashboardSummary {
  activeItemsCount: number
  purchasedThisWeekCount: number
  estimatedTotal: number
  recentItems: RecentItemRow[]
}

export interface RecentItemRow {
  id: string
  name: string
  quantity: number
  category?: string | null
  purchased: boolean
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
