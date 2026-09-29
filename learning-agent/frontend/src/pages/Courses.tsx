import { useEffect, useState } from 'react'
import { api } from '../api'
import type { Course, CourseStatus } from '../types'

export function Courses() {
  const [courses, setCourses] = useState<Course[]>([])
  const [error, setError] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [subject, setSubject] = useState('')

  const load = () => api.courses().then(setCourses).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!title.trim()) return
    setError(null)
    try {
      await api.createCourse({ title: title.trim(), subject: subject.trim() || null })
      setTitle(''); setSubject('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const setStatus = async (c: Course, status: CourseStatus) => {
    await api.updateCourse(c.id, { ...c, status })
    load()
  }

  const remove = async (c: Course) => {
    if (!confirm(`Delete "${c.title}"?`)) return
    await api.deleteCourse(c.id)
    load()
  }

  return (
    <div>
      <div className="page-head"><h1>Courses</h1><p>{courses.length} tracked</p></div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input style={{ flex: 2, minWidth: 180 }} placeholder="Course title (e.g. Intro to Algorithms)" value={title} onChange={(e) => setTitle(e.target.value)} />
          <input style={{ flex: 1, minWidth: 140 }} placeholder="Subject" value={subject} onChange={(e) => setSubject(e.target.value)} />
          <button className="btn primary" onClick={add}>Add</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {courses.map((c) => (
          <div className="item" key={c.id}>
            <div style={{ flex: 1 }}>
              <div className="row" style={{ gap: 8 }}>
                <span className="title">{c.title}</span>
                <span className={`pill ${c.status.toLowerCase()}`}>{c.status}</span>
              </div>
              <div className="sub">
                {c.subject ? c.subject : ''}{c.subject ? ' - ' : ''}started {new Date(c.startedAt).toLocaleDateString()}
                {c.notes ? ` - ${c.notes}` : ''}
              </div>
            </div>
            <div className="row" style={{ gap: 6 }}>
              {c.status !== 'Active' && <button className="btn ghost" onClick={() => setStatus(c, 'Active')}>Set Active</button>}
              {c.status !== 'Completed' && <button className="btn ghost" onClick={() => setStatus(c, 'Completed')}>Complete</button>}
              {c.status !== 'Paused' && <button className="btn ghost" onClick={() => setStatus(c, 'Paused')}>Pause</button>}
              <button className="btn ghost danger" onClick={() => remove(c)}>Delete</button>
            </div>
          </div>
        ))}
        {courses.length === 0 && <div className="empty">No courses yet.</div>}
      </div>
    </div>
  )
}
