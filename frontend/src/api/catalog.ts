import axios from 'axios'

const api = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL ?? '' })

export interface NodeDto {
  id: string
  type: string
  name: string
  description?: string
  properties?: Record<string, string>
}

export interface EdgeDto {
  id: string
  fromId: string
  toId: string
  relationType: string
}

export interface GraphDto {
  nodes: NodeDto[]
  edges: EdgeDto[]
}

export const catalogApi = {
  getGraph: () => api.get<GraphDto>('/api/graph').then(r => r.data),
  search: (q: string) => api.get<GraphDto>('/api/search', { params: { q } }).then(r => r.data),
  getNeighbors: (id: string) => api.get<GraphDto>(`/api/nodes/${id}/neighbors`).then(r => r.data),

  getNodes: () => api.get<NodeDto[]>('/api/nodes').then(r => r.data),
  createNode: (data: { type: string; name: string; description?: string; operatingSystem?: string; owner?: string; address?: string; dbType?: string; databaseAddress?: string; codeRepository?: string; documentationUrl?: string }) =>
    api.post<NodeDto>('/api/nodes', data).then(r => r.data),
  updateNode: (id: string, data: { name: string; description?: string; operatingSystem?: string; owner?: string; address?: string; dbType?: string; databaseAddress?: string; codeRepository?: string; documentationUrl?: string }) =>
    api.put<NodeDto>(`/api/nodes/${id}`, data).then(r => r.data),
  deleteNode: (id: string) => api.delete(`/api/nodes/${id}`),

  getEdges: () => api.get<EdgeDto[]>('/api/edges').then(r => r.data),
  createEdge: (data: { fromId: string; toId: string; relationType: string }) =>
    api.post<EdgeDto>('/api/edges', data).then(r => r.data),
  deleteEdge: (id: string) => api.delete(`/api/edges/${id}`),
}
