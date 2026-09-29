import { useEffect, useState } from 'react'
import { api } from '../../api'
import type { TaskItem, TaskPriority } from '../../types'

export function Tasks() {
  const [tasks, setTasks] = useState<TaskItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [due, setDue] = useState('')

  const load = () => api.tasks().then(setTasks).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!title.trim()) return
    setError(null)
    try {
      await api.createTask({
        title: title.trim(),
        priority,
        dueAt: due ? new Date(due).toISOString() : null,
      })
      setTitle(''); setDue(''); setPriority('Medium')
      load()
    } catch (e) { setError(String(e)) }
  }

  const toggle = async (t: TaskItem) => {
    await api.updateTask(t.id, { ...t, status: t.status === 'Done' ? 'Todo' : 'Done' })
    load()
  }

  const remove = async (t: TaskItem) => {
    if (!confirm(`Delete "${t.title}"?`)) return
    await api.deleteTask(t.id)
    load()
  }

  const open = tasks.filter((t) => t.status !== 'Done')
  const done = tasks.filter((t) => t.status === 'Done')

  return (
    <div>
      <div className="page-head"><h1>Tasks</h1><p>{open.length} open &middot; {done.length} done</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input
            style={{ flex: 2, minWidth: 200 }}
            placeholder="Add a task…"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && add()}
          />
          <select style={{ width: 130 }} value={priority} onChange={(e) => setPriority(e.target.value as TaskPriority)}>
            <option>Low</option><option>Medium</option><option>High</option>
          </select>
          <input style={{ width: 210 }} type="datetime-local" value={due} onChange={(e) => setDue(e.target.value)} />
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {open.map((t) => (
          <div className="item" key={t.id}>
            <div className={`check${t.status === 'Done' ? ' on' : ''}`} onClick={() => toggle(t)}>
              {t.status === 'Done' ? '✓' : ''}
            </div>
            <div style={{ flex: 1 }}>
              <div className="title">{t.title}</div>
              <div className="sub row" style={{ gap: 8 }}>
                <span className={`pill ${t.priority.toLowerCase()}`}>{t.priority}</span>
                {t.dueAt && <span>due {new Date(t.dueAt).toLocaleString([], { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })}</span>}
                {t.status === 'InProgress' && <span className="pill">In progress</span>}
              </div>
            </div>
            <button className="btn ghost danger" onClick={() => remove(t)}>Delete</button>
          </div>
        ))}
        {open.length === 0 && <div className="empty">No open tasks.</div>}
      </div>

      {done.length > 0 && (
        <>
          <h3 className="muted" style={{ margin: '24px 0 10px', fontSize: 13 }}>Completed</h3>
          <div className="list">
            {done.map((t) => (
              <div className="item checkdone" key={t.id}>
                <div className="check on" onClick={() => toggle(t)}>✓</div>
                <div style={{ flex: 1 }}><div className="title" style={{ textDecoration: 'line-through' }}>{t.title}</div></div>
                <button className="btn ghost danger" onClick={() => remove(t)}>Delete</button>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
