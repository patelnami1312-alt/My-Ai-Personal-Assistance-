import { useEffect, useMemo, useState } from 'react'
import { api } from '../../api'
import type { CalendarEvent } from '../../types'

function dayKey(iso: string) {
  return new Date(iso).toLocaleDateString([], { weekday: 'long', month: 'long', day: 'numeric' })
}

export function Calendar() {
  const [events, setEvents] = useState<CalendarEvent[]>([])
  const [error, setError] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [location, setLocation] = useState('')

  const load = () => {
    const now = new Date()
    const to = new Date(now.getTime() + 45 * 864e5)
    api.events(now.toISOString(), to.toISOString()).then(setEvents).catch((e) => setError(String(e)))
  }
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!title.trim() || !start) return
    setError(null)
    try {
      const s = new Date(start)
      const e = end ? new Date(end) : new Date(s.getTime() + 3600_000)
      await api.createEvent({
        title: title.trim(),
        startsAt: s.toISOString(),
        endsAt: e.toISOString(),
        location: location || null,
        allDay: false,
      })
      setTitle(''); setStart(''); setEnd(''); setLocation('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (ev: CalendarEvent) => {
    if (!confirm(`Delete "${ev.title}"?`)) return
    await api.deleteEvent(ev.id)
    load()
  }

  const grouped = useMemo(() => {
    const g: Record<string, CalendarEvent[]> = {}
    for (const e of events) (g[dayKey(e.startsAt)] ??= []).push(e)
    return Object.entries(g)
  }, [events])

  return (
    <div>
      <div className="page-head"><h1>Calendar</h1><p>Next 45 days &middot; {events.length} events</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="grid cols-2" style={{ gap: 10 }}>
          <div><label>Title</label><input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Meeting with…" /></div>
          <div><label>Location</label><input value={location} onChange={(e) => setLocation(e.target.value)} placeholder="Optional" /></div>
          <div><label>Starts</label><input type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} /></div>
          <div><label>Ends</label><input type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} /></div>
        </div>
        <button className="btn primary" style={{ marginTop: 12 }} onClick={add}>Add event</button>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      {grouped.length === 0 && <div className="empty">No upcoming events.</div>}
      {grouped.map(([day, evs]) => (
        <div key={day} style={{ marginBottom: 18 }}>
          <h3 className="muted" style={{ margin: '0 0 8px', fontSize: 13 }}>{day}</h3>
          <div className="list">
            {evs.map((e) => (
              <div className="item" key={e.id}>
                <div style={{ minWidth: 130 }} className="muted">
                  {new Date(e.startsAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}
                  {' – '}
                  {new Date(e.endsAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}
                </div>
                <div style={{ flex: 1 }}>
                  <div className="title">{e.title}</div>
                  {e.location && <div className="sub">{e.location}</div>}
                </div>
                <button className="btn ghost danger" onClick={() => remove(e)}>Delete</button>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
