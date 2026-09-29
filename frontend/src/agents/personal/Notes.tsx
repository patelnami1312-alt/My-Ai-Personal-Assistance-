import { useEffect, useState } from 'react'
import { api } from '../../api'
import type { Note } from '../../types'

export function Notes() {
  const [notes, setNotes] = useState<Note[]>([])
  const [q, setQ] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')

  const load = () => api.notes(q).then(setNotes).catch((e) => setError(String(e)))
  useEffect(() => { load() /* eslint-disable-next-line */ }, [q])

  const add = async () => {
    if (!title.trim() || !content.trim()) return
    setError(null)
    try {
      await api.createNote({ title: title.trim(), content: content.trim(), tags: [] })
      setTitle(''); setContent('')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (n: Note) => {
    if (!confirm(`Delete "${n.title}"?`)) return
    await api.deleteNote(n.id)
    load()
  }

  return (
    <div>
      <div className="page-head row between">
        <div><h1>Notes</h1><p>{notes.length} saved</p></div>
        <input style={{ width: 220 }} placeholder="Search notes…" value={q} onChange={(e) => setQ(e.target.value)} />
      </div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="field"><label>Title</label><input value={title} onChange={(e) => setTitle(e.target.value)} /></div>
        <div className="field"><label>Content</label><textarea value={content} onChange={(e) => setContent(e.target.value)} /></div>
        <button className="btn primary" onClick={add}>Save note</button>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="grid cols-2">
        {notes.map((n) => (
          <div className="card" key={n.id}>
            <div className="row between" style={{ marginBottom: 6 }}>
              <strong>{n.title}</strong>
              <button className="btn ghost danger" onClick={() => remove(n)}>Delete</button>
            </div>
            <div style={{ whiteSpace: 'pre-wrap', fontSize: 13, lineHeight: 1.55 }}>{n.content}</div>
            <div className="faint" style={{ marginTop: 8 }}>
              {new Date(n.createdAt).toLocaleDateString()}
              {n.tags.length > 0 && ' · ' + n.tags.join(', ')}
            </div>
          </div>
        ))}
        {notes.length === 0 && <div className="empty">No notes yet.</div>}
      </div>
    </div>
  )
}
