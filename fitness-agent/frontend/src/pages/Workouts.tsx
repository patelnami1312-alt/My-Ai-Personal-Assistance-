import { useEffect, useState } from 'react'
import { api } from '../api'
import type { WorkoutEntry } from '../types'

export function Workouts() {
  const [workouts, setWorkouts] = useState<WorkoutEntry[]>([])
  const [error, setError] = useState<string | null>(null)
  const [exercise, setExercise] = useState('')
  const [sets, setSets] = useState('')
  const [reps, setReps] = useState('')
  const [weight, setWeight] = useState('')

  const load = () => api.workouts().then(setWorkouts).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!exercise.trim()) return
    setError(null)
    try {
      await api.createWorkout({
        exercise: exercise.trim(),
        sets: sets ? Number(sets) : null,
        reps: reps ? Number(reps) : null,
        weightKg: weight ? Number(weight) : null,
      })
      setExercise(''); setSets(''); setReps(''); setWeight('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (w: WorkoutEntry) => {
    if (!confirm(`Delete "${w.exercise}"?`)) return
    await api.deleteWorkout(w.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Workouts</h1><p>{workouts.length} logged</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Exercise (e.g. Squat)" value={exercise} onChange={(e) => setExercise(e.target.value)} />
          <input style={{ width: 90 }} placeholder="Sets" type="number" value={sets} onChange={(e) => setSets(e.target.value)} />
          <input style={{ width: 90 }} placeholder="Reps" type="number" value={reps} onChange={(e) => setReps(e.target.value)} />
          <input style={{ width: 110 }} placeholder="Weight kg" type="number" value={weight} onChange={(e) => setWeight(e.target.value)} />
          <button className="btn primary" onClick={add}>Log</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {workouts.map((w) => (
          <div className="item" key={w.id}>
            <div style={{ minWidth: 90 }} className="muted">{new Date(w.date).toLocaleDateString()}</div>
            <div style={{ flex: 1 }}>
              <div className="title">{w.exercise}</div>
              <div className="sub">
                {w.sets && w.reps ? `${w.sets}x${w.reps}` : ''}{w.weightKg ? ` @ ${w.weightKg}kg` : ''}{w.durationMinutes ? ` - ${w.durationMinutes} min` : ''}
                {w.notes ? ` - ${w.notes}` : ''}
              </div>
            </div>
            <button className="btn ghost danger" onClick={() => remove(w)}>Delete</button>
          </div>
        ))}
        {workouts.length === 0 && <div className="empty">No workouts yet.</div>}
      </div>
    </div>
  )
}
