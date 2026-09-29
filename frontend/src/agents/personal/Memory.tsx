import { useEffect, useState } from 'react'
import { api } from '../../api'
import type { MemoryCategory, MemoryItem } from '../../types'

const CATEGORIES: MemoryCategory[] = ['Fact', 'Preference', 'Goal', 'Relationship', 'Event']

export function Memory() {
  const [memories, setMemories] = useState<MemoryItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [content, setContent] = useState('')
  const [category, setCategory] = useState<MemoryCategory>('Fact')

  const load = () => api.memories().then(setMemories).catch((e) => setError(String(e)))
  useEffect(() => { load() }, [])

  const add = async () => {
    if (!content.trim()) return
    setError(null)
    try {
      await api.createMemory({ content: content.trim(), category })
      setContent(''); setCategory('Fact')
      load()
    } catch (e) { setError(String(e)) }
  }

  const remove = async (m: MemoryItem) => {
    if (!confirm('Forget this memory?')) return
    await api.deleteMemory(m.id)
    load()
  }

  return (
    <div>
      <div className="page-head">
        <h1>Memory</h1>
        <p>What the assistant remembers about you. It also adds memories automatically from conversations.</p>
      </div>

      <div className="card" style={{ marginBottom: 18 }}>
        <div className="row wrap" style={{ gap: 10 }}>
          <input
            style={{ flex: 2, minWidth: 220 }}
            placeholder="e.g. I prefer morning meetings"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && add()}
          />
          <select style={{ width: 150 }} value={category} onChange={(e) => setCategory(e.target.value as MemoryCategory)}>
            {CATEGORIES.map((c) => <option key={c}>{c}</option>)}
          </select>
          <button className="btn primary" onClick={add}>Remember</button>
        </div>
      </div>

      {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="list">
        {memories.map((m) => (
          <div className="item" key={m.id}>
            <span className="pill">{m.category}</span>
            <div style={{ flex: 1 }}>
              <div className="title">{m.content}</div>
              <div className="sub">
                added {new Date(m.createdAt).toLocaleDateString()}
                {m.confidence < 1 && ` · confidence ${(m.confidence * 100).toFixed(0)}%`}
              </div>
            </div>
            <button className="btn ghost danger" onClick={() => remove(m)}>Forget</button>
          </div>
        ))}
        {memories.length === 0 && <div className="empty">No memories yet. Chat with the assistant or add one above.</div>}
      </div>
    </div>
  )
}
