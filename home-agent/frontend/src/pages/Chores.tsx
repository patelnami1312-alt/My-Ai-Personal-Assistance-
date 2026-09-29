import { useEffect, useState } from 'react'
import { api } from '../api'
import type { Chore, RecurrenceFrequency } from '../types'

export function Chores() {
  const [chores, setChores] = useState<Chore[]>([])
  const [error, setError] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [assignedTo, setAssignedTo] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [recurring, setRecurring] = useState(false)
  const [frequency, setFrequency] = useState<RecurrenceFrequency>('OneTime')

  const load = () => api.chores().then(setChores).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!title.trim()) return
    setError(null)
    try {
      await api.createChore({
        title: title.trim(),
        assignedTo: assignedTo || null,
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        recurring,
        frequency,
      })
      setTitle(''); setAssignedTo(''); setDueDate(''); setRecurring(false); setFrequency('OneTime')
      load()
    } catch (e) { setError(String(e)) }
  }

  const complete = async (c: Chore) => {
    await api.updateChore(c.id, { status: 'Done' })
    load()
  }

  const remove = async (c: Chore) => {
    if (!confirm(`Delete "${c.title}"?`)) return
    await api.deleteChore(c.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Chores</h1><p>{chores.length} total</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10, marginBottom: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Chore (e.g. Take out the trash)" value={title} onChange={(e) => setTitle(e.target.value)} />
          <input style={{ width: 140 }} placeholder="Assigned to" value={assignedTo} onChange={(e) => setAssignedTo(e.target.value)} />
          <input style={{ width: 160 }} type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </div>
        <div className="row wrap" style={{ gap: 10 }}>
          <label className="row" style={{ gap: 6, margin: 0, cursor: 'pointer' }}>
            <input type="checkbox" checked={recurring} onChange={(e) => setRecurring(e.target.checked)} style={{ width: 'auto' }} />
            <span className="faint">Recurring</span>
          </label>
          <select style={{ width: 140 }} value={frequency} disabled={!recurring} onChange={(e) => setFrequency(e.target.value as RecurrenceFrequency)}>
            <option value="OneTime">One-time</option>
            <option value="Daily">Daily</option>
            <option value="Weekly">Weekly</option>
            <option value="Monthly">Monthly</option>
          </select>
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {chores.map((c) => (
          <div className="item" key={c.id}>
            <div style={{ minWidth: 90 }} className="muted">{c.dueDate ? new Date(c.dueDate).toLocaleDateString() : '-'}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{c.title}</div>
              <div className="sub">
                {c.assignedTo ? `${c.assignedTo} - ` : ''}
                {c.recurring ? `recurring (${c.frequency})` : 'one-time'}
                {c.notes ? ` - ${c.notes}` : ''}
              </div>
            </div>
            <span className={`pill${c.status === 'Done' ? ' done' : ''}`}>{c.status}</span>
            {c.status === 'Todo' && <button className="btn ghost" onClick={() => complete(c)}>Complete</button>}
            <button className="btn ghost danger" onClick={() => remove(c)}>Delete</button>
          </div>
        ))}
        {chores.length === 0 && <div className="empty">No chores yet.</div>}
      </div>
    </div>
  )
}
