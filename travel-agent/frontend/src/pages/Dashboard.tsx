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
        <p>Your trips at a glance.</p>
      </div>

      {error && <div className="err">{error}</div>}
      {!summary && !error && <div className="card"><span className="spinner" /> Loading...</div>}

      {summary && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="grid cols-2">
            <div className="card stat"><span className="n">{summary.totalTrips}</span><span className="l">total trips</span></div>
            <div className="card stat">
              <span className="n">
                {summary.packingProgress ? `${summary.packingProgress.packed}/${summary.packingProgress.total}` : '-'}
              </span>
              <span className="l">packed for upcoming trip</span>
            </div>
          </div>

          <div className="card">
            <div className="row between" style={{ marginBottom: 12 }}>
              <strong>Upcoming trip</strong>
              <Link to="/trips" className="faint">Open trips &rarr;</Link>
            </div>
            {!summary.upcomingTrip && <div className="empty">No upcoming trips planned.</div>}
            {summary.upcomingTrip && (
              <Link to={`/trips/${summary.upcomingTrip.id}`} className="item clickable" style={{ textDecoration: 'none' }}>
                <div>
                  <div className="title">{summary.upcomingTrip.destination}</div>
                  <div className="sub">
                    {new Date(summary.upcomingTrip.startDate).toLocaleDateString()} - {new Date(summary.upcomingTrip.endDate).toLocaleDateString()}
                  </div>
                </div>
              </Link>
            )}
          </div>

          <div className="card row between">
            <span className="muted">Plan a trip, build an itinerary, or ask for travel advice.</span>
            <Link to="/chat" className="btn primary">Open chat</Link>
          </div>
        </div>
      )}
    </div>
  )
}
