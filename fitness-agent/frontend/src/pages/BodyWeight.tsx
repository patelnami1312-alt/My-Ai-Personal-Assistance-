import { useEffect, useState } from 'react'
import { api } from '../api'
import type { BodyWeightEntry } from '../types'

export function BodyWeight() {
  const [entries, setEntries] = useState<BodyWeightEntry[]>([])
  const [error, setError] = useState<string | null>(null)
  const [weight, setWeight] = useState('')
  const [notes, setNotes] = useState('')

  const load = () => api.bodyWeights().then(setEntries).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    const kg = Number(weight)
    if (!kg) return
    setError(null)
    try {
      await api.createBodyWeight({ weightKg: kg, notes: notes || null })
      setWeight(''); setNotes('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (e: BodyWeightEntry) => {
    if (!confirm(`Delete entry from ${new Date(e.date).toLocaleDateString()}?`)) return
    await api.deleteBodyWeight(e.id)
    load()
  }

  const latest = entries[0]
  const earliest = entries[entries.length - 1]
  const trend = latest && earliest && entries.length > 1 ? latest.weightKg - earliest.weightKg : null

  return (
    <div>
      <div className="page-head"><h1>Body Weight</h1><p>{entries.length} entries{trend != null ? ` - trend ${trend > 0 ? '+' : ''}${trend.toFixed(1)}kg` : ''}</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ width: 140 }} placeholder="Weight kg" type="number" step="0.1" value={weight} onChange={(e) => setWeight(e.target.value)} />
          <input style={{ flex: 1, minWidth: 200 }} placeholder="Notes (optional)" value={notes} onChange={(e) => setNotes(e.target.value)} />
          <button className="btn primary" onClick={add}>Log</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {entries.map((e) => (
          <div className="item" key={e.id}>
            <div style={{ minWidth: 90 }} className="muted">{new Date(e.date).toLocaleDateString()}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{e.weightKg}kg</div>
              {e.notes && <div className="sub">{e.notes}</div>}
            </div>
            <button className="btn ghost danger" onClick={() => remove(e)}>Delete</button>
          </div>
        ))}
        {entries.length === 0 && <div className="empty">No weight entries yet.</div>}
      </div>
    </div>
  )
}
