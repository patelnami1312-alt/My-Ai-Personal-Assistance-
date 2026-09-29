import { useEffect, useState } from 'react'
import { api } from '../api'
import type { FitnessGoal, GoalType } from '../types'

const TYPES: GoalType[] = ['WeightTarget', 'WorkoutFrequency', 'Custom']

export function Goals() {
  const [goals, setGoals] = useState<FitnessGoal[]>([])
  const [error, setError] = useState<string | null>(null)
  const [description, setDescription] = useState('')
  const [type, setType] = useState<GoalType>('Custom')
  const [targetValue, setTargetValue] = useState('')

  const load = () => api.goals().then(setGoals).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!description.trim()) return
    setError(null)
    try {
      await api.createGoal({ description: description.trim(), type, targetValue: targetValue ? Number(targetValue) : null })
      setDescription(''); setTargetValue(''); setType('Custom')
      load()
    } catch (e) { setError(String(e)) }
  }

  const markAchieved = async (g: FitnessGoal) => {
    await api.updateGoal(g.id, { description: g.description, type: g.type, targetValue: g.targetValue, targetDate: g.targetDate, status: 'Achieved' })
    load()
  }

  const remove = async (g: FitnessGoal) => {
    if (!confirm(`Delete goal "${g.description}"?`)) return
    await api.deleteGoal(g.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Goals</h1><p>{goals.filter((g) => g.status === 'Active').length} active</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 220 }} placeholder="e.g. Reach 70kg" value={description} onChange={(e) => setDescription(e.target.value)} />
          <select style={{ width: 170 }} value={type} onChange={(e) => setType(e.target.value as GoalType)}>
            {TYPES.map((t) => <option key={t}>{t}</option>)}
          </select>
          <input style={{ width: 130 }} placeholder="Target value" type="number" value={targetValue} onChange={(e) => setTargetValue(e.target.value)} />
          <button className="btn primary" onClick={add}>Add goal</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {goals.map((g) => (
          <div className="item" key={g.id}>
            <span className={`pill ${g.status.toLowerCase()}`}>{g.status}</span>
            <div style={{ flex: 1 }}>
              <div className="title">{g.description}</div>
              <div className="sub">{g.type}{g.targetValue != null ? ` - target ${g.targetValue}` : ''}</div>
            </div>
            {g.status === 'Active' && <button className="btn ghost" onClick={() => markAchieved(g)}>Mark achieved</button>}
            <button className="btn ghost danger" onClick={() => remove(g)}>Delete</button>
          </div>
        ))}
        {goals.length === 0 && <div className="empty">No goals yet.</div>}
      </div>
    </div>
  )
}
