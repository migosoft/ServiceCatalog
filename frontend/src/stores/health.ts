import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'

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

export const useHealthStore = defineStore('health', () => {
  const statuses = ref<Record<string, HealthStatus>>({})
  const configs  = ref<Record<string, HealthConfig>>({})
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

  const hasMonitored = computed(() => Object.keys(configs.value).length > 0)
  const anyDown = computed(() =>
    Object.entries(statuses.value).some(([id, s]) => configs.value[id] != null && !s.isAvailable)
  )

  function statusOf(nodeId: string): HealthStatus | undefined {
    return statuses.value[nodeId]
  }

  function configOf(nodeId: string): HealthConfig | undefined {
    return configs.value[nodeId]
  }

  return {
    statuses, configs,
    connect, disconnect,
    setConfig, deleteConfig,
    statusOf, configOf,
    hasMonitored, anyDown,
  }
})
