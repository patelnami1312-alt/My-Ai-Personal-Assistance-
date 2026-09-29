export interface ProjectDto {
  id: string
  name: string
  rootPath: string
  description?: string | null
  createdAt: string
  lastOpenedAt: string
}

export interface FileTreeNode {
  name: string
  path: string
  isDirectory: boolean
  size: number
  hasChildren: boolean
}

export interface FileContentDto {
  path: string
  text: string
  size: number
  lineCount: number
  language: string
  truncated: boolean
}
