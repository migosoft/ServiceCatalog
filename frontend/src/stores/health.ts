import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'
import { useCatalogStore } from './catalog'

const BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface HealthStatus {
  nodeId: string
  isAvailable: boolean
  checkedAt: string
  error?: string | null
}

export interface HealthConfig {
  nodeId: string
  checkType: string
  checkTarget: string
  intervalSeconds: number
  retryCount: number
}

export type NodeHealthDisplay = 'available' | 'compromised' | 'unavailable' | 'none'

export const useHealthStore = defineStore('health', () => {
  const statuses = ref<Record<string, HealthStatus>>({})
  const configs  = ref<Record<string, HealthConfig>>({})
  // Store reference obtained at setup time (not inside computed) for correct reactivity
  const catalogStore = useCatalogStore()
  let _es: EventSource | null = null
  let _reconnectTimer: ReturnType<typeof setTimeout> | null = null

  function connect() {
    if (_es) return
    _es = new EventSource(`${BASE}/api/health/stream`)
    _es.onmessage = (e: MessageEvent) => {
      try {
        const s: HealthStatus = JSON.parse(e.data)
        statuses.value = { ...statuses.value, [s.nodeId]: s }
      } catch {}
    }
    _es.onerror = () => {
      _es?.close()
      _es = null
      if (_reconnectTimer) clearTimeout(_reconnectTimer)
      _reconnectTimer = setTimeout(() => { connect() }, 5000)
    }
    loadConfigs()
  }

  function disconnect() {
    if (_reconnectTimer) clearTimeout(_reconnectTimer)
    _es?.close()
    _es = null
  }

  async function loadConfigs() {
    try {
      const r = await axios.get<HealthConfig[]>(`${BASE}/api/health/configs`)
      const m: Record<string, HealthConfig> = {}
      r.data.forEach(c => { m[c.nodeId] = c })
      configs.value = m
    } catch {}
  }

  async function setConfig(nodeId: string, req: { checkType: string; checkTarget: string; intervalSeconds: number; retryCount: number }) {
    await axios.put(`${BASE}/api/health/configs/${nodeId}`, req)
    configs.value = { ...configs.value, [nodeId]: { nodeId, ...req } }
  }

  async function deleteConfig(nodeId: string) {
    await axios.delete(`${BASE}/api/health/configs/${nodeId}`)
    const newC = { ...configs.value }
    delete newC[nodeId]
    configs.value = newC
    const newS = { ...statuses.value }
    delete newS[nodeId]
    statuses.value = newS
  }

  // Transitive compromise: walk the dependency graph backwards from every down node.
  // Edge direction A → B means "A depends on B" (REQUIRES / RUNS_ON).
  // If B is down/compromised, A is compromised — regardless of how many hops away.
  const compromisedIds = computed<Set<string>>(() => {
    const downIds = new Set(
      Object.entries(statuses.value)
        .filter(([, s]) => !s.isAvailable)
        .map(([id]) => id)
    )
    if (downIds.size === 0) return new Set()

    const edges = catalogStore.edges
    const result = new Set<string>()

    // BFS: for each affected node, find every node whose edge points TO it.
    // Those dependents become compromised and seed the next wave.
    const visited = new Set(downIds)
    const queue   = [...downIds]

    while (queue.length > 0) {
      const targetId = queue.shift()!
      for (const e of edges) {
        if (e.toId === targetId && !visited.has(e.fromId)) {
          visited.add(e.fromId)
          result.add(e.fromId)
          queue.push(e.fromId)
        }
      }
    }

    return result
  })

  const hasMonitored   = computed(() => Object.keys(configs.value).length > 0)
  const anyDown        = computed(() =>
    Object.entries(statuses.value).some(([id, s]) => configs.value[id] != null && !s.isAvailable)
  )
  const anyCompromised = computed(() => compromisedIds.value.size > 0)

  function statusOf(nodeId: string): HealthStatus | undefined {
    return statuses.value[nodeId]
  }

  function configOf(nodeId: string): HealthConfig | undefined {
    return configs.value[nodeId]
  }

  function displayStatus(nodeId: string): NodeHealthDisplay {
    const s = statuses.value[nodeId]
    if (s && !s.isAvailable) return 'unavailable'
    if (compromisedIds.value.has(nodeId)) return 'compromised'
    if (s?.isAvailable) return 'available'
    return 'none'
  }

  return {
    statuses, configs,
    connect, disconnect,
    setConfig, deleteConfig,
    statusOf, configOf,
    displayStatus,
    hasMonitored, anyDown, anyCompromised,
  }
})
