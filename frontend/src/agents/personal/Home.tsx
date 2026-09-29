import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../../api'
import { useVoice } from '../../hooks/useVoice'
import type { DailyBriefing } from '../../types'

function timeLabel(iso: string) {
  return new Date(iso).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
}

export function Home() {
  const [briefing, setBriefing] = useState<DailyBriefing | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const { speak, speaking, cancelSpeech, supported } = useVoice(() => {})

  useEffect(() => {
    api.briefing()
      .then(setBriefing)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load briefing'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <div className="page-head">
        <h1>{briefing?.greeting ?? 'Home'}</h1>
        <p>{briefing ? new Date(briefing.date).toLocaleDateString([], { weekday: 'long', month: 'long', day: 'numeric' }) : 'Your day at a glance'}</p>
      </div>

      {loading && <div className="card"><span className="spinner" /> Preparing your briefing…</div>}
      {error && <div className="err">{error}</div>}

      {briefing && (
        <div className="grid" style={{ gap: 16 }}>
          <div className="card">
            <div className="row between" style={{ marginBottom: 10 }}>
              <strong>Daily briefing</strong>
              {supported && (
                speaking
                  ? <button className="btn ghost" onClick={cancelSpeech}>◼ Stop</button>
                  : <button className="btn ghost" onClick={() => speak(briefing.summary)}>🔊 Play</button>
              )}
            </div>
            <p style={{ margin: 0, lineHeight: 1.6 }}>{briefing.summary}</p>
          </div>

          <div className="grid cols-3">
            <div className="card stat"><span className="n">{briefing.events.length}</span><span className="l">events today</span></div>
            <div className="card stat"><span className="n">{briefing.openTaskCount}</span><span className="l">open tasks</span></div>
            <div className="card stat">
              <span className="n" style={{ color: briefing.overdueTaskCount ? 'var(--red)' : undefined }}>
                {briefing.overdueTaskCount}
              </span>
              <span className="l">overdue</span>
            </div>
          </div>

          <div className="grid cols-2">
            <div className="card">
              <div className="row between" style={{ marginBottom: 12 }}>
                <strong>Today&rsquo;s schedule</strong>
                <Link to="/calendar" className="faint">Open calendar →</Link>
              </div>
              {briefing.events.length === 0 && <div className="empty">Nothing scheduled today.</div>}
              <div className="list">
                {briefing.events.map((e) => (
                  <div className="item" key={e.id}>
                    <div style={{ minWidth: 64 }} className="muted">{e.allDay ? 'All day' : timeLabel(e.startsAt)}</div>
                    <div>
                      <div className="title">{e.title}</div>
                      {e.location && <div className="sub">{e.location}</div>}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="card">
              <div className="row between" style={{ marginBottom: 12 }}>
                <strong>Focus tasks</strong>
                <Link to="/tasks" className="faint">Open tasks →</Link>
              </div>
              {briefing.tasks.length === 0 && <div className="empty">No tasks due. Nice.</div>}
              <div className="list">
                {briefing.tasks.map((t) => (
                  <div className="item" key={t.id}>
                    <span className={`pill ${t.priority.toLowerCase()}`}>{t.priority}</span>
                    <div>
                      <div className="title">{t.title}</div>
                      {t.dueAt && (
                        <div className="sub">
                          due {new Date(t.dueAt).toLocaleString([], { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="card row between">
            <span className="muted">Want to talk it through?</span>
            <Link to="/assistant" className="btn primary">Open assistant</Link>
          </div>
        </div>
      )}
    </div>
  )
}
