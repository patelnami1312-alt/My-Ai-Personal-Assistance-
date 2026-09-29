export type TripStatus = 'Planned' | 'Ongoing' | 'Completed'

export interface Trip {
  id: string
  destination: string
  startDate: string
  endDate: string
  status: TripStatus
  notes?: string | null
  createdAt: string
}

export type ItineraryItemType = 'Flight' | 'Hotel' | 'Activity' | 'Transport' | 'Other'

export interface ItineraryItem {
  id: string
  tripId: string
  title: string
  type: ItineraryItemType
  startsAt: string
  endsAt?: string | null
  location?: string | null
  notes?: string | null
  createdAt: string
}

export interface PackingItem {
  id: string
  tripId: string
  name: string
  category?: string | null
  packed: boolean
  createdAt: string
}

export interface UpcomingTripRow {
  id: string
  destination: string
  startDate: string
  endDate: string
}

export interface PackingProgressRow {
  packed: number
  total: number
}

export interface DashboardSummary {
  totalTrips: number
  upcomingTrip?: UpcomingTripRow | null
  packingProgress?: PackingProgressRow | null
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
