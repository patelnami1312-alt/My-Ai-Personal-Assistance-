import { useEffect, useState } from 'react'
import { api } from '../api'
import type { MaintenanceItem } from '../types'

export function Maintenance() {
  const [items, setItems] = useState<MaintenanceItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [intervalDays, setIntervalDays] = useState('')
  const [lastDone, setLastDone] = useState('')

  const load = () => api.maintenance().then(setItems).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!title.trim()) return
    setError(null)
    try {
      await api.createMaintenance({
        title: title.trim(),
        intervalDays: intervalDays ? Number(intervalDays) : null,
        lastDone: lastDone ? new Date(lastDone).toISOString() : null,
      })
      setTitle(''); setIntervalDays(''); setLastDone('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const markDone = async (m: MaintenanceItem) => {
    await api.markMaintenanceDone(m.id)
    load()
  }

  const remove = async (m: MaintenanceItem) => {
    if (!confirm(`Delete "${m.title}"?`)) return
    await api.deleteMaintenance(m.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Maintenance</h1><p>{items.length} tracked</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Item (e.g. Change HVAC filter)" value={title} onChange={(e) => setTitle(e.target.value)} />
          <input style={{ width: 140 }} placeholder="Interval (days)" type="number" value={intervalDays} onChange={(e) => setIntervalDays(e.target.value)} />
          <input style={{ width: 160 }} type="date" value={lastDone} onChange={(e) => setLastDone(e.target.value)} />
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {items.map((m) => (
          <div className="item" key={m.id}>
            <div style={{ minWidth: 90 }} className="muted">{m.nextDue ? new Date(m.nextDue).toLocaleDateString() : '-'}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{m.title}</div>
              <div className="sub">
                {m.lastDone ? `last done ${new Date(m.lastDone).toLocaleDateString()}` : 'never done'}
                {m.intervalDays ? ` - every ${m.intervalDays} days` : ''}
                {m.notes ? ` - ${m.notes}` : ''}
              </div>
            </div>
            <button className="btn ghost" onClick={() => markDone(m)}>Mark done</button>
            <button className="btn ghost danger" onClick={() => remove(m)}>Delete</button>
          </div>
        ))}
        {items.length === 0 && <div className="empty">No maintenance items yet.</div>}
      </div>
    </div>
  )
}
