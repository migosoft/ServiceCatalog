import { defineStore } from 'pinia'
import { ref } from 'vue'
import { catalogApi, type GraphDto, type NodeDto, type EdgeDto } from '@/api/catalog'

export const useCatalogStore = defineStore('catalog', () => {
  const nodes = ref<NodeDto[]>([])
  const edges = ref<EdgeDto[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function loadGraph() {
    loading.value = true
    error.value = null
    try {
      const graph = await catalogApi.getGraph()
      nodes.value = graph.nodes
      edges.value = graph.edges
    } catch (e) {
      error.value = 'Failed to load graph'
    } finally {
      loading.value = false
    }
  }

  async function search(q: string): Promise<GraphDto> {
    return catalogApi.search(q)
  }

  async function getNeighbors(id: string): Promise<GraphDto> {
    return catalogApi.getNeighbors(id)
  }

  async function createNode(data: { type: string; name: string; description?: string; operatingSystem?: string; owner?: string }) {
    const node = await catalogApi.createNode(data)
    nodes.value.push(node)
    return node
  }

  async function updateNode(id: string, data: { name: string; description?: string; operatingSystem?: string; owner?: string }) {
    const updated = await catalogApi.updateNode(id, data)
    const idx = nodes.value.findIndex(n => n.id === id)
    if (idx !== -1) nodes.value[idx] = updated
    return updated
  }

  async function deleteNode(id: string) {
    await catalogApi.deleteNode(id)
    nodes.value = nodes.value.filter(n => n.id !== id)
    edges.value = edges.value.filter(e => e.fromId !== id && e.toId !== id)
  }

  async function createEdge(data: { fromId: string; toId: string; relationType: string }) {
    const edge = await catalogApi.createEdge(data)
    edges.value.push(edge)
    return edge
  }

  async function deleteEdge(id: string) {
    await catalogApi.deleteEdge(id)
    edges.value = edges.value.filter(e => e.id !== id)
  }

  return { nodes, edges, loading, error, loadGraph, search, getNeighbors, createNode, updateNode, deleteNode, createEdge, deleteEdge }
})
