import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import type { ItineraryItem, ItineraryItemType, PackingItem, Trip } from '../types'

const itineraryTypes: ItineraryItemType[] = ['Flight', 'Hotel', 'Activity', 'Transport', 'Other']

export function TripDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [trip, setTrip] = useState<Trip | null>(null)
  const [itinerary, setItinerary] = useState<ItineraryItem[]>([])
  const [packing, setPacking] = useState<PackingItem[]>([])
  const [error, setError] = useState<string | null>(null)

  const [itTitle, setItTitle] = useState('')
  const [itType, setItType] = useState<ItineraryItemType>('Activity')
  const [itStartsAt, setItStartsAt] = useState('')
  const [itLocation, setItLocation] = useState('')

  const [packName, setPackName] = useState('')
  const [packCategory, setPackCategory] = useState('')

  const load = () => {
    if (!id) return
    api.trip(id).then(setTrip).catch((e) => setError(String(e)))
    api.itinerary(id).then(setItinerary).catch((e) => setError(String(e)))
    api.packingList(id).then(setPacking).catch((e) => setError(String(e)))
  }
  useEffect(() => { load() }, [id])

  const addItineraryItem = async () => {
    if (!id || !itTitle.trim() || !itStartsAt) return
    setError(null)
    try {
      await api.createItineraryItem({
        tripId: id,
        title: itTitle.trim(),
        type: itType,
        startsAt: new Date(itStartsAt).toISOString(),
        location: itLocation.trim() || null,
      })
      setItTitle(''); setItStartsAt(''); setItLocation('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const removeItineraryItem = async (item: ItineraryItem) => {
    if (!confirm(`Delete "${item.title}"?`)) return
    await api.deleteItineraryItem(item.id)
    load()
  }

  const addPackingItem = async () => {
    if (!id || !packName.trim()) return
    setError(null)
    try {
      await api.createPackingItem({ tripId: id, name: packName.trim(), category: packCategory.trim() || null })
      setPackName(''); setPackCategory('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const togglePacked = async (item: PackingItem) => {
    await api.updatePackingItem(item.id, { packed: !item.packed })
    load()
  }

  const removePackingItem = async (item: PackingItem) => {
    if (!confirm(`Delete "${item.name}"?`)) return
    await api.deletePackingItem(item.id)
    load()
  }

  const deleteTrip = async () => {
    if (!trip || !id) return
    if (!confirm(`Delete trip to "${trip.destination}"? This also deletes its itinerary and packing list.`)) return
    await api.deleteTrip(id)
    navigate('/trips')
  }

  if (!trip) {
    return (
      <div>
        {error && <div className="err">{error}</div>}
        {!error && <div className="card"><span className="spinner" /> Loading...</div>}
      </div>
    )
  }

  return (
    <div>
      <div className="page-head row between">
        <div>
          <Link to="/trips" className="faint">&larr; All trips</Link>
          <h1>{trip.destination}</h1>
          <p>
            {new Date(trip.startDate).toLocaleDateString()} - {new Date(trip.endDate).toLocaleDateString()}
            {' '}<span className={`pill ${trip.status.toLowerCase()}`}>{trip.status}</span>
          </p>
        </div>
        <button className="btn ghost danger" onClick={deleteTrip}>Delete trip</button>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}
      {trip.notes && <div className="card" style={{ marginBottom: 18 }}><span className="muted">{trip.notes}</span></div>}

      <div className="grid cols-2">
        <div>
          <h2 style={{ fontSize: 16 }}>Itinerary</h2>
          <div className="card" style={{ marginBottom: 14 }}>
            <div className="row wrap" style={{ gap: 10 }}>
              <input style={{ flex: 2, minWidth: 140 }} placeholder="Title" value={itTitle} onChange={(e) => setItTitle(e.target.value)} />
              <select style={{ width: 130 }} value={itType} onChange={(e) => setItType(e.target.value as ItineraryItemType)}>
                {itineraryTypes.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
              <input style={{ width: 190 }} type="datetime-local" value={itStartsAt} onChange={(e) => setItStartsAt(e.target.value)} />
              <input style={{ flex: 1, minWidth: 120 }} placeholder="Location" value={itLocation} onChange={(e) => setItLocation(e.target.value)} />
              <button className="btn primary" onClick={addItineraryItem}>Add</button>
            </div>
          </div>
          <div className="list">
            {itinerary.map((item) => (
              <div className="item" key={item.id}>
                <div style={{ flex: 1 }}>
                  <div className="title">{item.title} <span className="faint">[{item.type}]</span></div>
                  <div className="sub">
                    {new Date(item.startsAt).toLocaleString()}
                    {item.location ? ` - ${item.location}` : ''}
                  </div>
                </div>
                <button className="btn ghost danger" onClick={() => removeItineraryItem(item)}>Delete</button>
              </div>
            ))}
            {itinerary.length === 0 && <div className="empty">No itinerary items yet.</div>}
          </div>
        </div>

        <div>
          <h2 style={{ fontSize: 16 }}>Packing list</h2>
          <div className="card" style={{ marginBottom: 14 }}>
            <div className="row wrap" style={{ gap: 10 }}>
              <input style={{ flex: 2, minWidth: 140 }} placeholder="Item (e.g. Passport)" value={packName} onChange={(e) => setPackName(e.target.value)} />
              <input style={{ flex: 1, minWidth: 120 }} placeholder="Category" value={packCategory} onChange={(e) => setPackCategory(e.target.value)} />
              <button className="btn primary" onClick={addPackingItem}>Add</button>
            </div>
          </div>
          <div className="list">
            {packing.map((item) => (
              <div className="item" key={item.id}>
                <input type="checkbox" checked={item.packed} onChange={() => togglePacked(item)} style={{ width: 'auto', marginTop: 3 }} />
                <div style={{ flex: 1 }}>
                  <div className="title" style={item.packed ? { textDecoration: 'line-through', color: 'var(--text-dim)' } : undefined}>
                    {item.name}
                  </div>
                  {item.category && <div className="sub">{item.category}</div>}
                </div>
                <button className="btn ghost danger" onClick={() => removePackingItem(item)}>Delete</button>
              </div>
            ))}
            {packing.length === 0 && <div className="empty">No packing items yet.</div>}
          </div>
        </div>
      </div>
    </div>
  )
}
