import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import type { DashboardSummary } from '../types'

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
        <p>Your home at a glance.</p>
      </div>

      {error && <div className="err">{error}</div>}
      {!summary && !error && <div className="card"><span className="spinner" /> Loading...</div>}

      {summary && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="grid cols-4">
            <div className="card stat"><span className="n">{summary.choresDueTodayCount}</span><span className="l">chores due today</span></div>
            <div className="card stat">
              <span className="n" style={{ color: summary.overdueChoresCount > 0 ? 'var(--red)' : undefined }}>{summary.overdueChoresCount}</span>
              <span className="l">overdue chores</span>
            </div>
            <div className="card stat">
              <span className="n" style={{ color: summary.lowInventoryCount > 0 ? 'var(--amber)' : undefined }}>{summary.lowInventoryCount}</span>
              <span className="l">low inventory items</span>
            </div>
            <div className="card stat"><span className="n">{summary.upcomingMaintenance.length}</span><span className="l">maintenance due soon</span></div>
          </div>

          <div className="grid cols-2">
            <div className="card">
              <div className="row between" style={{ marginBottom: 12 }}>
                <strong>Open chores</strong>
                <Link to="/chores" className="faint">Open chores &rarr;</Link>
              </div>
              {summary.recentChores.length === 0 && <div className="empty">No open chores.</div>}
              <div className="list">
                {summary.recentChores.map((c) => (
                  <div className="item" key={c.id}>
                    <div style={{ minWidth: 90 }} className="muted">
                      {c.dueDate ? new Date(c.dueDate).toLocaleDateString() : '-'}
                    </div>
                    <div className="title">{c.title}</div>
                  </div>
                ))}
              </div>
            </div>

            <div className="card">
              <div className="row between" style={{ marginBottom: 12 }}>
                <strong>Upcoming maintenance</strong>
                <Link to="/maintenance" className="faint">Open maintenance &rarr;</Link>
              </div>
              {summary.upcomingMaintenance.length === 0 && <div className="empty">Nothing due in the next 30 days.</div>}
              <div className="list">
                {summary.upcomingMaintenance.map((m) => (
                  <div className="item" key={m.id}>
                    <div style={{ minWidth: 90 }} className="muted">
                      {m.nextDue ? new Date(m.nextDue).toLocaleDateString() : '-'}
                    </div>
                    <div className="title">{m.title}</div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="card row between">
            <span className="muted">Add a chore, log maintenance, or update supplies.</span>
            <Link to="/chat" className="btn primary">Open chat</Link>
          </div>
        </div>
      )}
    </div>
  )
}
