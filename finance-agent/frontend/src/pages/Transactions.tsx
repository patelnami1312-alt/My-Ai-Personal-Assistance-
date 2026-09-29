import { useEffect, useState } from 'react'
import { api } from '../api'
import type { Transaction, TransactionType } from '../types'

export function Transactions() {
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [error, setError] = useState<string | null>(null)
  const [description, setDescription] = useState('')
  const [amount, setAmount] = useState('')
  const [type, setType] = useState<TransactionType>('Expense')
  const [category, setCategory] = useState('')

  const load = () => api.transactions().then(setTransactions).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!description.trim() || !amount || !category.trim()) return
    setError(null)
    try {
      await api.createTransaction({
        description: description.trim(),
        amount: Number(amount),
        type,
        category: category.trim(),
      })
      setDescription(''); setAmount(''); setCategory('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (t: Transaction) => {
    if (!confirm(`Delete "${t.description}"?`)) return
    await api.deleteTransaction(t.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Transactions</h1><p>{transactions.length} logged</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
          <input style={{ width: 110 }} placeholder="Amount" type="number" value={amount} onChange={(e) => setAmount(e.target.value)} />
          <select style={{ width: 120 }} value={type} onChange={(e) => setType(e.target.value as TransactionType)}>
            <option value="Expense">Expense</option>
            <option value="Income">Income</option>
          </select>
          <input style={{ width: 150 }} placeholder="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
          <button className="btn primary" onClick={add}>Log</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {transactions.map((t) => (
          <div className="item" key={t.id}>
            <div style={{ minWidth: 90 }} className="muted">{new Date(t.date).toLocaleDateString()}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{t.description}</div>
              <div className="sub">{t.category}{t.notes ? ` - ${t.notes}` : ''}</div>
            </div>
            <div className="title" style={{ color: t.type === 'Income' ? 'var(--green)' : 'var(--red)' }}>
              {t.type === 'Income' ? '+' : '-'}${t.amount.toFixed(2)}
            </div>
            <button className="btn ghost danger" onClick={() => remove(t)}>Delete</button>
          </div>
        ))}
        {transactions.length === 0 && <div className="empty">No transactions yet.</div>}
      </div>
    </div>
  )
}
