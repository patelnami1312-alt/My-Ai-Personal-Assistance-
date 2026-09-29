import type { FileContentDto, FileTreeNode, ProjectDto } from './types'

const BASE = '/api'

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(BASE + path, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(`${res.status} ${res.statusText}${text ? ` — ${text}` : ''}`)
  }
  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const api = {
  health: () => http<{ status: string; time: string }>('/health'),

  projects: () => http<ProjectDto[]>('/projects'),
  registerProject: (body: { name: string; rootPath: string; description?: string }) =>
    http<ProjectDto>('/projects', { method: 'POST', body: JSON.stringify(body) }),
  openProject: (id: string) => http<ProjectDto>(`/projects/${id}`),
  unregisterProject: (id: string) => http<void>(`/projects/${id}`, { method: 'DELETE' }),
  projectTree: (id: string, path?: string) =>
    http<FileTreeNode[]>(`/projects/${id}/tree${path ? `?path=${encodeURIComponent(path)}` : ''}`),
  projectFile: (id: string, path: string) =>
    http<FileContentDto>(`/projects/${id}/file?path=${encodeURIComponent(path)}`),
}
