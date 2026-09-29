export type TransactionType = 'Income' | 'Expense'

export interface Transaction {
  id: string
  date: string
  description: string
  amount: number
  type: TransactionType
  category: string
  notes?: string | null
  createdAt: string
}

export type AccountType = 'Checking' | 'Savings' | 'Credit' | 'Cash' | 'Investment'

export interface FinanceAccount {
  id: string
  name: string
  type: AccountType
  balance: number
  notes?: string | null
  createdAt: string
}

export interface Budget {
  id: string
  category: string
  monthlyLimit: number
  createdAt: string
}

export type BillFrequency = 'OneTime' | 'Weekly' | 'Monthly' | 'Yearly'

export interface Bill {
  id: string
  name: string
  amount: number
  dueDate: string
  recurring: boolean
  frequency: BillFrequency
  paid: boolean
  notes?: string | null
  createdAt: string
}

export interface UpcomingBillRow {
  id: string
  name: string
  amount: number
  dueDate: string
}

export interface BudgetStatusRow {
  category: string
  limit: number
  spent: number
}

export interface DashboardSummary {
  totalIncomeThisMonth: number
  totalExpenseThisMonth: number
  netThisMonth: number
  totalAccountBalance: number
  upcomingBills: UpcomingBillRow[]
  budgetStatus: BudgetStatusRow[]
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
