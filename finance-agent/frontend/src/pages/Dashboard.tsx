import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import type { DashboardSummary } from '../types'

function money(n: number) {
  return `$${n.toFixed(2)}`
}

export function Dashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.dashboard().then(setSummary).catch((e) => setError(e instanceof Error ? e.message : 'Failed to load'))
  }, [])

  return (
    <div>
      <div className="page-head">
        <h1>Dashboard</h1>
        <p>Your finances at a glance.</p>
      </div>

      {error && <div className="err">{error}</div>}
      {!summary && !error && <div className="card"><span className="spinner" /> Loading...</div>}

      {summary && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="grid cols-4">
            <div className="card stat"><span className="n">{money(summary.totalIncomeThisMonth)}</span><span className="l">income this month</span></div>
            <div className="card stat"><span className="n">{money(summary.totalExpenseThisMonth)}</span><span className="l">expenses this month</span></div>
            <div className="card stat">
              <span className="n" style={{ color: summary.netThisMonth < 0 ? 'var(--red)' : 'var(--green)' }}>
                {summary.netThisMonth >= 0 ? '+' : ''}{money(summary.netThisMonth)}
              </span>
              <span className="l">net this month</span>
            </div>
            <div className="card stat"><span className="n">{money(summary.totalAccountBalance)}</span><span className="l">total account balance</span></div>
          </div>

          <div className="grid cols-2">
            <div className="card">
              <div className="row between" style={{ marginBottom: 12 }}>
                <strong>Upcoming bills</strong>
                <Link to="/bills" className="faint">Open bills &rarr;</Link>
              </div>
              {summary.upcomingBills.length === 0 && <div className="empty">No bills due in the next 14 days.</div>}
              <div className="list">
                {summary.upcomingBills.map((b) => (
                  <div className="item" key={b.id}>
                    <div style={{ minWidth: 90 }} className="muted">{new Date(b.dueDate).toLocaleDateString()}</div>
                    <div style={{ flex: 1 }}>
                      <div className="title">{b.name}</div>
                    </div>
                    <div className="title">{money(b.amount)}</div>
                  </div>
                ))}
              </div>
            </div>

            <div className="card">
              <div className="row between" style={{ marginBottom: 12 }}>
                <strong>Budget status</strong>
                <Link to="/budgets" className="faint">Open budgets &rarr;</Link>
              </div>
              {summary.budgetStatus.length === 0 && <div className="empty">No budgets set yet.</div>}
              <div className="list">
                {summary.budgetStatus.map((b) => {
                  const pct = b.limit > 0 ? Math.min(100, (b.spent / b.limit) * 100) : 0
                  const over = b.spent > b.limit
                  return (
                    <div className="item" style={{ display: 'block' }} key={b.category}>
                      <div className="row between">
                        <div className="title">{b.category}</div>
                        <div className="sub">{money(b.spent)} / {money(b.limit)}</div>
                      </div>
                      <div className="bar"><div className={`fill${over ? ' over' : ''}`} style={{ width: `${pct}%` }} /></div>
                    </div>
                  )
                })}
              </div>
            </div>
          </div>

          <div className="card row between">
            <span className="muted">Log a transaction, a bill, or ask for advice.</span>
            <Link to="/chat" className="btn primary">Open chat</Link>
          </div>
        </div>
      )}
    </div>
  )
}
