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
        <p>Your shopping list at a glance.</p>
      </div>

      {error && <div className="err">{error}</div>}
      {!summary && !error && <div className="card"><span className="spinner" /> Loading...</div>}

      {summary && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="grid cols-4">
            <div className="card stat"><span className="n">{summary.activeItemsCount}</span><span className="l">items on your list</span></div>
            <div className="card stat"><span className="n">{summary.purchasedThisWeekCount}</span><span className="l">purchased this week</span></div>
            <div className="card stat"><span className="n">${summary.estimatedTotal.toFixed(2)}</span><span className="l">estimated total</span></div>
            <div className="card stat"><span className="n">{summary.recentItems.length}</span><span className="l">recent items</span></div>
          </div>

          <div className="card">
            <div className="row between" style={{ marginBottom: 12 }}>
              <strong>Recent items</strong>
              <Link to="/list" className="faint">Open shopping list &rarr;</Link>
            </div>
            {summary.recentItems.length === 0 && <div className="empty">No items yet.</div>}
            <div className="list">
              {summary.recentItems.map((i) => (
                <div className="item" key={i.id}>
                  <div>
                    <div className="title">{i.name} {i.quantity > 1 ? `x${i.quantity}` : ''}</div>
                    <div className="sub">
                      {i.category ? i.category : ''}{i.purchased ? ' - purchased' : ''}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="card row between">
            <span className="muted">Add an item, mark something purchased, or ask for advice.</span>
            <Link to="/chat" className="btn primary">Open chat</Link>
          </div>
        </div>
      )}
    </div>
  )
}
