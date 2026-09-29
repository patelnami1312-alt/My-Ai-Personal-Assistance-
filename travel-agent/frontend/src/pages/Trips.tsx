import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api'
import type { Trip } from '../types'

export function Trips() {
  const [trips, setTrips] = useState<Trip[]>([])
  const [error, setError] = useState<string | null>(null)
  const [destination, setDestination] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [notes, setNotes] = useState('')

  const load = () => api.trips().then(setTrips).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!destination.trim() || !startDate || !endDate) return
    setError(null)
    try {
      await api.createTrip({
        destination: destination.trim(),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        notes: notes.trim() || null,
      })
      setDestination(''); setStartDate(''); setEndDate(''); setNotes('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (t: Trip) => {
    if (!confirm(`Delete trip to "${t.destination}"? This also deletes its itinerary and packing list.`)) return
    await api.deleteTrip(t.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Trips</h1><p>{trips.length} planned</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Destination (e.g. Tokyo, Japan)" value={destination} onChange={(e) => setDestination(e.target.value)} />
          <input style={{ width: 160 }} type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          <input style={{ width: 160 }} type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          <input style={{ flex: 2, minWidth: 160 }} placeholder="Notes (optional)" value={notes} onChange={(e) => setNotes(e.target.value)} />
          <button className="btn primary" onClick={add}>Add trip</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {trips.map((t) => (
          <div className="item" key={t.id}>
            <Link to={`/trips/${t.id}`} style={{ flex: 1, textDecoration: 'none', color: 'inherit' }}>
              <div className="title">{t.destination}</div>
              <div className="sub">
                {new Date(t.startDate).toLocaleDateString()} - {new Date(t.endDate).toLocaleDateString()}
                {' '}<span className={`pill ${t.status.toLowerCase()}`}>{t.status}</span>
                {t.notes ? ` - ${t.notes}` : ''}
              </div>
            </Link>
            <button className="btn ghost danger" onClick={() => remove(t)}>Delete</button>
          </div>
        ))}
        {trips.length === 0 && <div className="empty">No trips yet.</div>}
      </div>
    </div>
  )
}
