import { useEffect, useState } from 'react'
import { api } from '../api'
import type { Bill, BillFrequency } from '../types'

export function Bills() {
  const [bills, setBills] = useState<Bill[]>([])
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [amount, setAmount] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [recurring, setRecurring] = useState(false)
  const [frequency, setFrequency] = useState<BillFrequency>('Monthly')

  const load = () => api.bills().then(setBills).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!name.trim() || !amount || !dueDate) return
    setError(null)
    try {
      await api.createBill({
        name: name.trim(),
        amount: Number(amount),
        dueDate: new Date(dueDate).toISOString(),
        recurring,
        frequency: recurring ? frequency : 'OneTime',
      })
      setName(''); setAmount(''); setDueDate(''); setRecurring(false)
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (b: Bill) => {
    if (!confirm(`Delete bill "${b.name}"?`)) return
    await api.deleteBill(b.id)
    load()
  }

  const markPaid = async (b: Bill) => {
    await api.updateBill(b.id, { ...b, paid: true })
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Bills</h1><p>{bills.length} tracked</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Bill name" value={name} onChange={(e) => setName(e.target.value)} />
          <input style={{ width: 110 }} placeholder="Amount" type="number" value={amount} onChange={(e) => setAmount(e.target.value)} />
          <input style={{ width: 160 }} type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
          <label className="row" style={{ gap: 6, margin: 0, cursor: 'pointer' }}>
            <input type="checkbox" checked={recurring} onChange={(e) => setRecurring(e.target.checked)} style={{ width: 'auto' }} />
            <span className="faint">Recurring</span>
          </label>
          {recurring && (
            <select style={{ width: 120 }} value={frequency} onChange={(e) => setFrequency(e.target.value as BillFrequency)}>
              <option value="Weekly">Weekly</option>
              <option value="Monthly">Monthly</option>
              <option value="Yearly">Yearly</option>
            </select>
          )}
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {bills.map((b) => (
          <div className="item" key={b.id}>
            <div style={{ minWidth: 90 }} className="muted">{new Date(b.dueDate).toLocaleDateString()}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{b.name}</div>
              <div className="sub">{b.recurring ? `Recurring (${b.frequency})` : 'One-time'}{b.notes ? ` - ${b.notes}` : ''}</div>
            </div>
            <span className={`pill ${b.paid ? 'paid' : 'unpaid'}`}>{b.paid ? 'Paid' : 'Unpaid'}</span>
            <div className="title">${b.amount.toFixed(2)}</div>
            {!b.paid && <button className="btn ghost" onClick={() => markPaid(b)}>Mark paid</button>}
            <button className="btn ghost danger" onClick={() => remove(b)}>Delete</button>
          </div>
        ))}
        {bills.length === 0 && <div className="empty">No bills yet.</div>}
      </div>
    </div>
  )
}
