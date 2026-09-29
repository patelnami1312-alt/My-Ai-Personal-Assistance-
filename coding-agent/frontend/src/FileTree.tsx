import { useCallback, useEffect, useState } from 'react'
import { api } from './api'
import type { FileTreeNode } from './types'

interface NodeProps {
  projectId: string
  node: FileTreeNode
  depth: number
  selectedPath: string | null
  onSelectFile: (path: string) => void
}

function TreeNode({ projectId, node, depth, selectedPath, onSelectFile }: NodeProps) {
  const [open, setOpen] = useState(false)
  const [children, setChildren] = useState<FileTreeNode[] | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const toggle = useCallback(async () => {
    if (!node.isDirectory) {
      onSelectFile(node.path)
      return
    }
    const next = !open
    setOpen(next)
    if (next && children === null) {
      setLoading(true)
      try {
        setChildren(await api.projectTree(projectId, node.path))
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load')
      } finally {
        setLoading(false)
      }
    }
  }, [node, open, children, projectId, onSelectFile])

  const isSelected = selectedPath === node.path

  return (
    <div>
      <div
        className={`tree-row${isSelected ? ' sel' : ''}`}
        style={{ paddingLeft: 8 + depth * 14 }}
        onClick={toggle}
      >
        <span className="tree-caret">
          {node.isDirectory ? (open ? '▾' : '▸') : ''}
        </span>
        <span className="tree-ico">{node.isDirectory ? (open ? '📂' : '📁') : '📄'}</span>
        <span className="tree-name">{node.name}</span>
        {!node.isDirectory && node.size > 0 && (
          <span className="tree-size">{formatSize(node.size)}</span>
        )}
      </div>
      {open && (
        <div>
          {loading && <div className="tree-row muted" style={{ paddingLeft: 8 + (depth + 1) * 14 }}>…</div>}
          {error && <div className="tree-row" style={{ paddingLeft: 8 + (depth + 1) * 14, color: 'var(--red)' }}>{error}</div>}
          {children?.map((c) => (
            <TreeNode
              key={c.path}
              projectId={projectId}
              node={c}
              depth={depth + 1}
              selectedPath={selectedPath}
              onSelectFile={onSelectFile}
            />
          ))}
          {children?.length === 0 && (
            <div className="tree-row muted" style={{ paddingLeft: 8 + (depth + 1) * 14 }}>empty</div>
          )}
        </div>
      )}
    </div>
  )
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

export function FileTree({
  projectId,
  selectedPath,
  onSelectFile,
}: {
  projectId: string
  selectedPath: string | null
  onSelectFile: (path: string) => void
}) {
  const [roots, setRoots] = useState<FileTreeNode[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setRoots(null)
    setError(null)
    api.projectTree(projectId)
      .then(setRoots)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load project'))
  }, [projectId])

  if (error) return <div className="err">{error}</div>
  if (!roots) return <div className="muted" style={{ padding: 12 }}><span className="spinner" /> Loading files…</div>

  return (
    <div className="tree">
      {roots.map((n) => (
        <TreeNode
          key={n.path}
          projectId={projectId}
          node={n}
          depth={0}
          selectedPath={selectedPath}
          onSelectFile={onSelectFile}
        />
      ))}
    </div>
  )
}
