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
        <p>Your learning progress at a glance.</p>
      </div>

      {error && <div className="err">{error}</div>}
      {!summary && !error && <div className="card"><span className="spinner" /> Loading...</div>}

      {summary && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="grid cols-4">
            <div className="card stat"><span className="n">{summary.studyMinutesThisWeek}</span><span className="l">minutes studied this week</span></div>
            <div className="card stat"><span className="n">{summary.activeCoursesCount}</span><span className="l">active courses</span></div>
            <div className="card stat"><span className="n">{summary.activeGoalsCount}</span><span className="l">active goals</span></div>
            <div className="card stat"><span className="n">{summary.recentSessions.length}</span><span className="l">recent sessions</span></div>
          </div>

          <div className="card">
            <div className="row between" style={{ marginBottom: 12 }}>
              <strong>Recent study sessions</strong>
              <Link to="/study-log" className="faint">Open study log &rarr;</Link>
            </div>
            {summary.recentSessions.length === 0 && <div className="empty">No study sessions logged yet.</div>}
            <div className="list">
              {summary.recentSessions.map((s) => (
                <div className="item" key={s.id}>
                  <div style={{ minWidth: 90 }} className="muted">{new Date(s.date).toLocaleDateString()}</div>
                  <div>
                    <div className="title">{s.topic}</div>
                    <div className="sub">{s.durationMinutes} min</div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="card row between">
            <span className="muted">Log a study session, save a note, or ask for advice.</span>
            <Link to="/chat" className="btn primary">Open chat</Link>
          </div>
        </div>
      )}
    </div>
  )
}
