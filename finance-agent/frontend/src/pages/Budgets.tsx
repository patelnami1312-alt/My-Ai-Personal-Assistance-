import { useEffect, useState } from 'react'
import { api } from '../api'
import type { Budget } from '../types'

export function Budgets() {
  const [budgets, setBudgets] = useState<Budget[]>([])
  const [error, setError] = useState<string | null>(null)
  const [category, setCategory] = useState('')
  const [limit, setLimit] = useState('')

  const load = () => api.budgets().then(setBudgets).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!category.trim() || !limit) return
    setError(null)
    try {
      await api.createBudget({ category: category.trim(), monthlyLimit: Number(limit) })
      setCategory(''); setLimit('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (b: Budget) => {
    if (!confirm(`Delete budget "${b.category}"?`)) return
    await api.deleteBudget(b.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Budgets</h1><p>{budgets.length} categories</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Category (e.g. Groceries)" value={category} onChange={(e) => setCategory(e.target.value)} />
          <input style={{ width: 150 }} placeholder="Monthly limit" type="number" value={limit} onChange={(e) => setLimit(e.target.value)} />
          <button className="btn primary" onClick={add}>Set budget</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {budgets.map((b) => (
          <div className="item" key={b.id}>
            <div style={{ flex: 1 }}>
              <div className="title">{b.category}</div>
            </div>
            <div className="title">${b.monthlyLimit.toFixed(2)}/mo</div>
            <button className="btn ghost danger" onClick={() => remove(b)}>Delete</button>
          </div>
        ))}
        {budgets.length === 0 && <div className="empty">No budgets set yet.</div>}
      </div>
    </div>
  )
}
