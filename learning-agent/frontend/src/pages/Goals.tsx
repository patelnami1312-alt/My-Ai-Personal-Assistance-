import { useEffect, useState } from 'react'
import { api } from '../api'
import type { LearningGoal, LearningGoalStatus } from '../types'

export function Goals() {
  const [goals, setGoals] = useState<LearningGoal[]>([])
  const [error, setError] = useState<string | null>(null)
  const [description, setDescription] = useState('')
  const [targetDate, setTargetDate] = useState('')

  const load = () => api.goals().then(setGoals).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!description.trim()) return
    setError(null)
    try {
      await api.createGoal({
        description: description.trim(),
        targetDate: targetDate ? new Date(targetDate).toISOString() : null,
      })
      setDescription(''); setTargetDate('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const setStatus = async (g: LearningGoal, status: LearningGoalStatus) => {
    await api.updateGoal(g.id, { ...g, status })
    load()
  }

  const remove = async (g: LearningGoal) => {
    if (!confirm(`Delete goal "${g.description}"?`)) return
    await api.deleteGoal(g.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Goals</h1><p>{goals.length} tracked</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 220 }} placeholder="Goal, e.g. Finish the React course" value={description} onChange={(e) => setDescription(e.target.value)} />
          <input style={{ width: 170 }} type="date" value={targetDate} onChange={(e) => setTargetDate(e.target.value)} />
          <button className="btn primary" onClick={add}>Set goal</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {goals.map((g) => (
          <div className="item" key={g.id}>
            <div style={{ flex: 1 }}>
              <div className="row" style={{ gap: 8 }}>
                <span className="title">{g.description}</span>
                <span className={`pill ${g.status.toLowerCase()}`}>{g.status}</span>
              </div>
              {g.targetDate && <div className="sub">by {new Date(g.targetDate).toLocaleDateString()}</div>}
            </div>
            <div className="row" style={{ gap: 6 }}>
              {g.status !== 'Active' && <button className="btn ghost" onClick={() => setStatus(g, 'Active')}>Reactivate</button>}
              {g.status !== 'Achieved' && <button className="btn ghost" onClick={() => setStatus(g, 'Achieved')}>Mark achieved</button>}
              {g.status !== 'Abandoned' && <button className="btn ghost" onClick={() => setStatus(g, 'Abandoned')}>Abandon</button>}
              <button className="btn ghost danger" onClick={() => remove(g)}>Delete</button>
            </div>
          </div>
        ))}
        {goals.length === 0 && <div className="empty">No goals yet.</div>}
      </div>
    </div>
  )
}
