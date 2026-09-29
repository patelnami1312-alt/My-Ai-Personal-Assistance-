import { useEffect, useState } from 'react'
import { api } from '../api'
import type { StudySession } from '../types'

export function StudyLog() {
  const [sessions, setSessions] = useState<StudySession[]>([])
  const [error, setError] = useState<string | null>(null)
  const [topic, setTopic] = useState('')
  const [duration, setDuration] = useState('')

  const load = () => api.studySessions().then(setSessions).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!topic.trim() || !duration) return
    setError(null)
    try {
      await api.createStudySession({
        topic: topic.trim(),
        durationMinutes: Number(duration),
      })
      setTopic(''); setDuration('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (s: StudySession) => {
    if (!confirm(`Delete "${s.topic}" session?`)) return
    await api.deleteStudySession(s.id)
    load()
  }

  const totalMinutes = sessions.reduce((sum, s) => sum + s.durationMinutes, 0)

  return (
    <div>
      <div className="page-head"><h1>Study Log</h1><p>{sessions.length} sessions - {totalMinutes} total minutes</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Topic (e.g. React hooks)" value={topic} onChange={(e) => setTopic(e.target.value)} />
          <input style={{ width: 140 }} placeholder="Duration (min)" type="number" value={duration} onChange={(e) => setDuration(e.target.value)} />
          <button className="btn primary" onClick={add}>Log</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {sessions.map((s) => (
          <div className="item" key={s.id}>
            <div style={{ minWidth: 90 }} className="muted">{new Date(s.date).toLocaleDateString()}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{s.topic}</div>
              <div className="sub">
                {s.durationMinutes} min{s.notes ? ` - ${s.notes}` : ''}
              </div>
            </div>
            <button className="btn ghost danger" onClick={() => remove(s)}>Delete</button>
          </div>
        ))}
        {sessions.length === 0 && <div className="empty">No study sessions yet.</div>}
      </div>
    </div>
  )
}
