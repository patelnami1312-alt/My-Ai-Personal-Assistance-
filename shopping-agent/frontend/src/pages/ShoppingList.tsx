import { useEffect, useState } from 'react'
import { api } from '../api'
import type { ShoppingItem } from '../types'

export function ShoppingList() {
  const [items, setItems] = useState<ShoppingItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [quantity, setQuantity] = useState('')
  const [category, setCategory] = useState('')
  const [store, setStore] = useState('')
  const [price, setPrice] = useState('')

  const load = () => api.items(false).then(setItems).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!name.trim()) return
    setError(null)
    try {
      await api.createItem({
        name: name.trim(),
        quantity: quantity ? Number(quantity) : 1,
        category: category || null,
        store: store || null,
        estimatedPrice: price ? Number(price) : null,
      })
      setName(''); setQuantity(''); setCategory(''); setStore(''); setPrice('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const markPurchased = async (i: ShoppingItem) => {
    await api.updateItem(i.id, { purchased: true })
    load()
  }

  const remove = async (i: ShoppingItem) => {
    if (!confirm(`Delete "${i.name}"?`)) return
    await api.deleteItem(i.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Shopping List</h1><p>{items.length} items to buy</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Item (e.g. Milk)" value={name} onChange={(e) => setName(e.target.value)} />
          <input style={{ width: 90 }} placeholder="Qty" type="number" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
          <input style={{ width: 130 }} placeholder="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
          <input style={{ width: 130 }} placeholder="Store" value={store} onChange={(e) => setStore(e.target.value)} />
          <input style={{ width: 110 }} placeholder="Est. price" type="number" value={price} onChange={(e) => setPrice(e.target.value)} />
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {items.map((i) => (
          <div className="item" key={i.id}>
            <div style={{ flex: 1 }}>
              <div className="title">{i.name} {i.quantity > 1 ? `x${i.quantity}` : ''}</div>
              <div className="sub">
                {i.category ? `${i.category}` : ''}{i.store ? ` - ${i.store}` : ''}{i.estimatedPrice != null ? ` - est. $${i.estimatedPrice.toFixed(2)}` : ''}
                {i.notes ? ` - ${i.notes}` : ''}
              </div>
            </div>
            <button className="btn ghost" onClick={() => markPurchased(i)}>Mark purchased</button>
            <button className="btn ghost danger" onClick={() => remove(i)}>Delete</button>
          </div>
        ))}
        {items.length === 0 && <div className="empty">Your shopping list is empty.</div>}
      </div>
    </div>
  )
}
