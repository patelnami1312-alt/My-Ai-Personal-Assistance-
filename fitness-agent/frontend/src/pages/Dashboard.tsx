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
        <p>Your training at a glance.</p>
      </div>

      {error && <div className="err">{error}</div>}
      {!summary && !error && <div className="card"><span className="spinner" /> Loading...</div>}

      {summary && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="grid cols-4">
            <div className="card stat"><span className="n">{summary.workoutsThisWeek}</span><span className="l">workouts this week</span></div>
            <div className="card stat">
              <span className="n">{summary.latestWeightKg != null ? `${summary.latestWeightKg}kg` : '-'}</span>
              <span className="l">latest weight</span>
            </div>
            <div className="card stat">
              <span className="n" style={{ color: summary.weightChange30dKg != null && summary.weightChange30dKg < 0 ? 'var(--green)' : undefined }}>
                {summary.weightChange30dKg != null ? `${summary.weightChange30dKg > 0 ? '+' : ''}${summary.weightChange30dKg.toFixed(1)}kg` : '-'}
              </span>
              <span className="l">change (30d)</span>
            </div>
            <div className="card stat"><span className="n">{summary.activeGoals}</span><span className="l">active goals</span></div>
          </div>

          <div className="card">
            <div className="row between" style={{ marginBottom: 12 }}>
              <strong>Recent workouts</strong>
              <Link to="/workouts" className="faint">Open workouts &rarr;</Link>
            </div>
            {summary.recentWorkouts.length === 0 && <div className="empty">No workouts logged yet.</div>}
            <div className="list">
              {summary.recentWorkouts.map((w) => (
                <div className="item" key={w.id}>
                  <div style={{ minWidth: 90 }} className="muted">{new Date(w.date).toLocaleDateString()}</div>
                  <div>
                    <div className="title">{w.exercise}</div>
                    {(w.sets || w.reps || w.weightKg) && (
                      <div className="sub">
                        {w.sets && w.reps ? `${w.sets}x${w.reps}` : ''}{w.weightKg ? ` @ ${w.weightKg}kg` : ''}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="card row between">
            <span className="muted">Log a workout, a meal, or ask for advice.</span>
            <Link to="/chat" className="btn primary">Open chat</Link>
          </div>
        </div>
      )}
    </div>
  )
}
