import { useEffect, useMemo, useState } from 'react'
import { api } from './api'
import { FileTree } from './FileTree'
import type { FileContentDto, ProjectDto } from './types'

const LS_KEY = 'coding-agent:last-project'

export default function App() {
  const [projects, setProjects] = useState<ProjectDto[]>([])
  const [activeId, setActiveId] = useState<string | null>(null)
  const [selectedPath, setSelectedPath] = useState<string | null>(null)
  const [file, setFile] = useState<FileContentDto | null>(null)
  const [fileLoading, setFileLoading] = useState(false)
  const [fileError, setFileError] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [showRegister, setShowRegister] = useState(false)

  const active = useMemo(() => projects.find((p) => p.id === activeId) ?? null, [projects, activeId])

  const loadProjects = async (selectId?: string) => {
    try {
      const list = await api.projects()
      setProjects(list)
      const target = selectId ?? activeId ?? localStorage.getItem(LS_KEY) ?? list[0]?.id ?? null
      if (target && list.some((p) => p.id === target)) setActiveId(target)
      else setActiveId(list[0]?.id ?? null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load projects')
    }
  }

  useEffect(() => { loadProjects() /* eslint-disable-next-line */ }, [])

  useEffect(() => {
    if (activeId) localStorage.setItem(LS_KEY, activeId)
    setSelectedPath(null)
    setFile(null)
    setFileError(null)
  }, [activeId])

  useEffect(() => {
    if (!activeId || !selectedPath) return
    setFileLoading(true)
    setFileError(null)
    api.projectFile(activeId, selectedPath)
      .then(setFile)
      .catch((e) => { setFile(null); setFileError(e instanceof Error ? e.message : 'Failed to read file') })
      .finally(() => setFileLoading(false))
  }, [activeId, selectedPath])

  const removeProject = async (p: ProjectDto) => {
    if (!confirm(`Unregister "${p.name}"? This only removes it here — your files are untouched.`)) return
    await api.unregisterProject(p.id)
    if (activeId === p.id) setActiveId(null)
    loadProjects()
  }

  return (
    <div className="app">
      <div className="topbar">
        <div className="brand"><span className="dot">⌥</span><span>Coding Agent</span></div>
        <div className="row wrap" style={{ justifyContent: 'flex-end' }}>
          <a className="btn ghost" href="http://localhost:5173" target="_blank" rel="noreferrer">✨ Personal ↗</a>
          <a className="btn ghost" href="http://localhost:5175" target="_blank" rel="noreferrer">🏋 Fitness ↗</a>
          <a className="btn ghost" href="http://localhost:5176" target="_blank" rel="noreferrer">💰 Finance ↗</a>
          <a className="btn ghost" href="http://localhost:5177" target="_blank" rel="noreferrer">🏠 Home ↗</a>
          <a className="btn ghost" href="http://localhost:5178" target="_blank" rel="noreferrer">🛒 Shopping ↗</a>
          <a className="btn ghost" href="http://localhost:5179" target="_blank" rel="noreferrer">🎓 Learning ↗</a>
          <a className="btn ghost" href="http://localhost:5180" target="_blank" rel="noreferrer">✈ Travel ↗</a>
          <button className="btn primary" onClick={() => setShowRegister(true)}>+ Register project</button>
        </div>
      </div>

      <main className="content">
        {error && <div className="err" style={{ marginBottom: 14 }}>{error}</div>}

        {projects.length === 0 ? (
          <div className="card empty">
            No projects yet. Click <strong>Register project</strong> and point it at a folder on this machine.
          </div>
        ) : (
          <>
            <div className="row wrap" style={{ gap: 8, marginBottom: 16 }}>
              {projects.map((p) => (
                <button
                  key={p.id}
                  className={`btn${p.id === activeId ? ' primary' : ''}`}
                  onClick={() => setActiveId(p.id)}
                  title={p.rootPath}
                >
                  {p.name}
                </button>
              ))}
            </div>

            {active && (
              <>
                <div className="row between" style={{ marginBottom: 10 }}>
                  <span className="faint" style={{ fontFamily: 'monospace' }}>{active.rootPath}</span>
                  <button className="btn ghost danger" onClick={() => removeProject(active)}>Unregister</button>
                </div>

                <div className="workspace">
                  <div className="card explorer">
                    <div className="explorer-head">Files</div>
                    <FileTree projectId={active.id} selectedPath={selectedPath} onSelectFile={setSelectedPath} />
                  </div>

                  <div className="card viewer">
                    {!selectedPath && <div className="empty">Select a file to view its contents.</div>}
                    {selectedPath && (
                      <>
                        <div className="viewer-head row between">
                          <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{selectedPath}</span>
                          {file && (
                            <span className="faint">
                              {file.language} · {file.lineCount} lines{file.truncated ? ' · truncated' : ''}
                            </span>
                          )}
                        </div>
                        {fileLoading && <div className="muted" style={{ padding: 14 }}><span className="spinner" /> Loading…</div>}
                        {fileError && <div className="err" style={{ margin: 12 }}>{fileError}</div>}
                        {file && <CodeView text={file.text} />}
                      </>
                    )}
                  </div>
                </div>
              </>
            )}
          </>
        )}
      </main>

      {showRegister && (
        <RegisterModal
          onClose={() => setShowRegister(false)}
          onDone={(p) => { setShowRegister(false); loadProjects(p.id) }}
        />
      )}
    </div>
  )
}

function CodeView({ text }: { text: string }) {
  const lines = text.split('\n')
  return (
    <div className="code-scroll">
      <table className="code">
        <tbody>
          {lines.map((line, i) => (
            <tr key={i}>
              <td className="ln">{i + 1}</td>
              <td className="lc">{line || '\u00a0'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function RegisterModal({ onClose, onDone }: { onClose: () => void; onDone: (p: ProjectDto) => void }) {
  const [name, setName] = useState('')
  const [rootPath, setRootPath] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async () => {
    if (!name.trim() || !rootPath.trim()) return
    setBusy(true)
    setError(null)
    try {
      const p = await api.registerProject({
        name: name.trim(),
        rootPath: rootPath.trim(),
        description: description.trim() || undefined,
      })
      onDone(p)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to register')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-back" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h3>Register a project</h3>
        <div className="field">
          <label>Name</label>
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="My App" autoFocus />
        </div>
        <div className="field">
          <label>Absolute folder path on this machine</label>
          <input
            value={rootPath}
            onChange={(e) => setRootPath(e.target.value)}
            placeholder="C:\Users\you\code\my-app  or  /home/you/code/my-app"
            style={{ fontFamily: 'monospace' }}
          />
        </div>
        <div className="field">
          <label>Description (optional)</label>
          <input value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>
        {error && <div className="err" style={{ marginBottom: 12 }}>{error}</div>}
        <div className="row between">
          <button className="btn ghost" onClick={onClose}>Cancel</button>
          <button className="btn primary" disabled={busy || !name.trim() || !rootPath.trim()} onClick={submit}>
            {busy ? 'Checking…' : 'Register'}
          </button>
        </div>
      </div>
    </div>
  )
}
