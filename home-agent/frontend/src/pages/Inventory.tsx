import { useEffect, useState } from 'react'
import { api } from '../api'
import type { InventoryItem } from '../types'

export function Inventory() {
  const [items, setItems] = useState<InventoryItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [category, setCategory] = useState('')
  const [quantity, setQuantity] = useState('')
  const [lowThreshold, setLowThreshold] = useState('')

  const load = () => api.inventory().then(setItems).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!name.trim() || quantity === '') return
    setError(null)
    try {
      await api.createInventoryItem({
        name: name.trim(),
        category: category || null,
        quantity: Number(quantity),
        lowThreshold: lowThreshold ? Number(lowThreshold) : 1,
      })
      setName(''); setCategory(''); setQuantity(''); setLowThreshold('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const updateQuantity = async (i: InventoryItem, delta: number) => {
    const next = Math.max(0, i.quantity + delta)
    await api.updateInventoryItem(i.id, { ...i, quantity: next })
    load()
  }

  const remove = async (i: InventoryItem) => {
    if (!confirm(`Delete "${i.name}"?`)) return
    await api.deleteInventoryItem(i.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Inventory</h1><p>{items.length} items</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Item (e.g. Paper towels)" value={name} onChange={(e) => setName(e.target.value)} />
          <input style={{ width: 140 }} placeholder="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
          <input style={{ width: 110 }} placeholder="Quantity" type="number" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
          <input style={{ width: 130 }} placeholder="Low threshold" type="number" value={lowThreshold} onChange={(e) => setLowThreshold(e.target.value)} />
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {items.map((i) => (
          <div className="item" key={i.id}>
            <div style={{ flex: 1 }}>
              <div className="title">{i.name}</div>
              <div className="sub">
                {i.category ? `${i.category} - ` : ''}qty {i.quantity}
                {i.notes ? ` - ${i.notes}` : ''}
              </div>
            </div>
            {i.quantity <= i.lowThreshold && <span className="pill low">LOW</span>}
            <button className="btn ghost" onClick={() => updateQuantity(i, -1)}>-1</button>
            <button className="btn ghost" onClick={() => updateQuantity(i, 1)}>+1</button>
            <button className="btn ghost danger" onClick={() => remove(i)}>Delete</button>
          </div>
        ))}
        {items.length === 0 && <div className="empty">No inventory items yet.</div>}
      </div>
    </div>
  )
}
