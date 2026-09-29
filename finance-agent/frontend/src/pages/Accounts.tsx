import { useEffect, useState } from 'react'
import { api } from '../api'
import type { AccountType, FinanceAccount } from '../types'

export function Accounts() {
  const [accounts, setAccounts] = useState<FinanceAccount[]>([])
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [type, setType] = useState<AccountType>('Checking')
  const [balance, setBalance] = useState('')

  const load = () => api.accounts().then(setAccounts).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!name.trim() || !balance) return
    setError(null)
    try {
      await api.createAccount({ name: name.trim(), type, balance: Number(balance) })
      setName(''); setBalance('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (a: FinanceAccount) => {
    if (!confirm(`Delete account "${a.name}"?`)) return
    await api.deleteAccount(a.id)
    load()
  }

  const editBalance = async (a: FinanceAccount) => {
    const next = prompt(`New balance for ${a.name}`, String(a.balance))
    if (next === null) return
    const n = Number(next)
    if (Number.isNaN(n)) return
    await api.updateAccount(a.id, { name: a.name, type: a.type, balance: n, notes: a.notes })
    load()
  }

  const total = accounts.reduce((s, a) => s + a.balance, 0)

  return (
    <div>
      <div className="page-head"><h1>Accounts</h1><p>{accounts.length} accounts &bull; total ${total.toFixed(2)}</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Account name" value={name} onChange={(e) => setName(e.target.value)} />
          <select style={{ width: 140 }} value={type} onChange={(e) => setType(e.target.value as AccountType)}>
            <option value="Checking">Checking</option>
            <option value="Savings">Savings</option>
            <option value="Credit">Credit</option>
            <option value="Cash">Cash</option>
            <option value="Investment">Investment</option>
          </select>
          <input style={{ width: 130 }} placeholder="Balance" type="number" value={balance} onChange={(e) => setBalance(e.target.value)} />
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {accounts.map((a) => (
          <div className="item" key={a.id}>
            <div style={{ flex: 1 }}>
              <div className="title">{a.name}</div>
              <div className="sub">{a.type}{a.notes ? ` - ${a.notes}` : ''}</div>
            </div>
            <div className="title">${a.balance.toFixed(2)}</div>
            <button className="btn ghost" onClick={() => editBalance(a)}>Edit</button>
            <button className="btn ghost danger" onClick={() => remove(a)}>Delete</button>
          </div>
        ))}
        {accounts.length === 0 && <div className="empty">No accounts yet.</div>}
      </div>
    </div>
  )
}
