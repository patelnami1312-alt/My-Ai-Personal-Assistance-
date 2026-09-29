import { useEffect, useState } from 'react'
import { api } from '../api'
import type { NutritionEntry } from '../types'

export function Nutrition() {
  const [meals, setMeals] = useState<NutritionEntry[]>([])
  const [error, setError] = useState<string | null>(null)
  const [description, setDescription] = useState('')
  const [calories, setCalories] = useState('')

  const load = () => api.meals().then(setMeals).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!description.trim()) return
    setError(null)
    try {
      await api.createMeal({ description: description.trim(), calories: calories ? Number(calories) : null })
      setDescription(''); setCalories('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (m: NutritionEntry) => {
    if (!confirm(`Delete "${m.description}"?`)) return
    await api.deleteMeal(m.id)
    load()
  }

  const todayCalories = meals
    .filter((m) => new Date(m.date).toDateString() === new Date().toDateString())
    .reduce((sum, m) => sum + (m.calories ?? 0), 0)

  return (
    <div>
      <div className="page-head"><h1>Nutrition</h1><p>{meals.length} logged - {todayCalories} kcal today</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 220 }} placeholder="What did you eat?" value={description} onChange={(e) => setDescription(e.target.value)} />
          <input style={{ width: 130 }} placeholder="Calories" type="number" value={calories} onChange={(e) => setCalories(e.target.value)} />
          <button className="btn primary" onClick={add}>Log</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {meals.map((m) => (
          <div className="item" key={m.id}>
            <div style={{ minWidth: 140 }} className="muted">{new Date(m.date).toLocaleString([], { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{m.description}</div>
              {(m.calories || m.proteinG || m.carbsG || m.fatG) && (
                <div className="sub">
                  {m.calories ? `${m.calories} kcal` : ''}
                  {m.proteinG ? ` - P${m.proteinG}g` : ''}
                  {m.carbsG ? ` C${m.carbsG}g` : ''}
                  {m.fatG ? ` F${m.fatG}g` : ''}
                </div>
              )}
            </div>
            <button className="btn ghost danger" onClick={() => remove(m)}>Delete</button>
          </div>
        ))}
        {meals.length === 0 && <div className="empty">No meals logged yet.</div>}
      </div>
    </div>
  )
}
