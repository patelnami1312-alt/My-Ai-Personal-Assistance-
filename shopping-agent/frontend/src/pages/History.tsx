import { useEffect, useState } from 'react'
import { api } from '../api'
import type { ShoppingItem } from '../types'

export function History() {
  const [items, setItems] = useState<ShoppingItem[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = () => api.items(true).then(setItems).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const remove = async (i: ShoppingItem) => {
    if (!confirm(`Delete "${i.name}" from history?`)) return
    await api.deleteItem(i.id)
    load()
  }

  const sorted = [...items].sort((a, b) => (b.purchasedAt ?? '').localeCompare(a.purchasedAt ?? ''))

  return (
    <div>
      <div className="page-head"><h1>History</h1><p>{items.length} items purchased</p></div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {sorted.map((i) => (
          <div className="item" key={i.id}>
            <div style={{ minWidth: 110 }} className="muted">
              {i.purchasedAt ? new Date(i.purchasedAt).toLocaleDateString() : ''}
            </div>
            <div style={{ flex: 1 }}>
              <div className="title">{i.name} {i.quantity > 1 ? `x${i.quantity}` : ''}</div>
              <div className="sub">
                {i.category ? `${i.category}` : ''}{i.store ? ` - ${i.store}` : ''}{i.estimatedPrice != null ? ` - $${i.estimatedPrice.toFixed(2)}` : ''}
              </div>
            </div>
            <button className="btn ghost danger" onClick={() => remove(i)}>Delete</button>
          </div>
        ))}
        {items.length === 0 && <div className="empty">Nothing purchased yet.</div>}
      </div>
    </div>
  )
}
