<template>
  <aside class="inspector">
    <!-- Header -->
    <div class="insp-header">
      <div class="insp-title-row">
        <div class="node-icon" :class="node.type.toLowerCase()">
          <SvcIcon v-if="node.type === 'Service'" />
          <SrvIcon v-else-if="node.type === 'Server'" />
          <DbIcon  v-else />
        </div>
        <div class="insp-title-text">
          <div class="insp-type">{{ node.type }}</div>
          <div class="insp-name">{{ node.name }}</div>
          <div v-if="node.description" class="insp-desc">{{ node.description }}</div>
        </div>
        <button class="close-btn" @click="$emit('close')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M6 6l12 12M18 6 6 18"/></svg>
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="tabs">
      <button
        v-for="t in TABS"
        :key="t.id"
        :class="['tab-btn', { active: tab === t.id }]"
        @click="tab = t.id"
      >
        {{ t.label }}
        <span v-if="t.id === 'connections'" class="tab-count">{{ allRels.length }}</span>
        <span v-if="t.id === 'monitor' && nodeDispStatus !== 'none'"
              class="tab-health-dot"
              :class="nodeDispStatus" />
      </button>
    </div>

    <!-- Content -->
    <div class="insp-body">
      <!-- Overview tab -->
      <template v-if="tab === 'overview'">
        <div class="section">
          <div class="section-label">Identity</div>
          <div class="readonly-rows">
            <div class="readonly-row"><span class="rr-key">id</span><span class="rr-val">{{ node.id }}</span></div>
            <div class="readonly-row"><span class="rr-key">type</span><span class="rr-val">{{ node.type }}</span></div>
          </div>
        </div>

        <div class="section">
          <div class="section-label">Details</div>
          <div class="editable-rows">
            <div class="editable-row">
              <label class="er-label">Name</label>
              <input
                :value="node.name"
                class="er-input"
                @change="patch('name', ($event.target as HTMLInputElement).value)"
              />
            </div>
            <div class="editable-row">
              <label class="er-label">Owner</label>
              <input
                :value="node.properties?.['owner'] ?? ''"
                class="er-input"
                placeholder="—"
                @change="patch('owner', ($event.target as HTMLInputElement).value)"
              />
            </div>
            <div class="editable-row">
              <label class="er-label">Address</label>
              <input
                :value="node.properties?.['address'] ?? ''"
                class="er-input"
                placeholder="—"
                @change="patch('address', ($event.target as HTMLInputElement).value)"
              />
            </div>
            <div v-if="node.type === 'Database'" class="editable-row">
              <label class="er-label">Type</label>
              <select
                :value="node.properties?.['db_type'] ?? ''"
                class="er-input er-select"
                @change="patch('db_type', ($event.target as HTMLSelectElement).value)"
              >
                <option value="">—</option>
                <option v-for="opt in DB_TYPES" :key="opt" :value="opt">{{ opt }}</option>
              </select>
            </div>
            <div v-if="node.type === 'Server'" class="editable-row">
              <label class="er-label">OS</label>
              <select
                :value="node.properties?.['os'] ?? ''"
                class="er-input er-select"
                @change="patch('os', ($event.target as HTMLSelectElement).value)"
              >
                <option value="">—</option>
                <option value="Windows">Windows</option>
                <option value="Linux">Linux</option>
              </select>
            </div>
            <div class="editable-row">
              <label class="er-label">Description</label>
              <textarea
                :value="node.description ?? ''"
                class="er-input er-textarea"
                rows="2"
                @change="patch('description', ($event.target as HTMLTextAreaElement).value)"
              />
            </div>
          </div>
        </div>


      </template>

      <!-- Monitor tab -->
      <template v-if="tab === 'monitor'">
        <div v-if="nodeStatus" class="section">
          <div class="section-label">Current status</div>
          <div class="health-status-row" :class="nodeDispStatus">
            <span class="health-dot" :class="nodeDispStatus" />
            <span class="health-label">
              {{ nodeDispStatus === 'available' ? 'Available'
               : nodeDispStatus === 'compromised' ? 'Compromised'
               : 'Down' }}
            </span>
            <span class="health-time">{{ formatAge(nodeStatus.checkedAt) }}</span>
          </div>
          <div v-if="nodeStatus.error" class="health-err-text">{{ nodeStatus.error }}</div>
        </div>

        <div class="section">
          <div class="section-label-row">
            <span class="section-label">{{ nodeConfig ? 'Configuration' : 'Enable monitoring' }}</span>
            <span class="check-type-badge" :class="node.type === 'Server' ? 'ping' : 'http'">
              {{ node.type === 'Server' ? 'ICMP Ping' : 'HTTP GET' }}
            </span>
          </div>
          <div class="editable-rows">
            <div class="editable-row">
              <label class="er-label">{{ node.type === 'Server' ? 'Address' : 'URL' }}</label>
              <input v-model="monitorForm.checkTarget" class="er-input"
                     :placeholder="node.type === 'Server' ? 'hostname or IP' : 'https://…/health'" />
            </div>
            <div class="editable-row">
              <label class="er-label">Interval</label>
              <select v-model.number="monitorForm.intervalSeconds" class="er-input er-select">
                <option :value="15">15 s</option>
                <option :value="30">30 s</option>
                <option :value="60">1 min</option>
                <option :value="300">5 min</option>
                <option :value="900">15 min</option>
              </select>
            </div>
            <div class="editable-row">
              <label class="er-label">Retries</label>
              <select v-model.number="monitorForm.retryCount" class="er-input er-select">
                <option :value="0">0</option>
                <option :value="1">1</option>
                <option :value="2">2</option>
                <option :value="3">3</option>
                <option :value="5">5</option>
              </select>
            </div>
          </div>
        </div>

        <p v-if="monitorError" class="error-msg">{{ monitorError }}</p>

        <div class="monitor-btns">
          <button class="btn-save" :disabled="!monitorForm.checkTarget.trim() || monitorSubmitting" @click="saveMonitor">
            {{ nodeConfig ? 'Update' : 'Enable' }}
          </button>
          <button v-if="nodeConfig" class="btn-remove" :disabled="monitorSubmitting" @click="removeMonitor">
            Remove
          </button>
        </div>
      </template>

      <!-- Connections tab -->
      <template v-if="tab === 'connections'">
        <template v-for="rt in ['REQUIRES', 'RUNS_ON']" :key="rt">
          <template v-if="byType[rt].out.length || byType[rt].in.length">
            <div class="section">
              <div class="rel-type-header">
                <span class="rel-line" :class="rt === 'REQUIRES' ? 'dashed' : 'solid'" />
                <span class="rel-type-label">{{ rt }}</span>
              </div>
              <div v-if="byType[rt].out.length">
                <div class="rel-dir-label">this node → others</div>
                <div class="conn-list">
                  <div v-for="r in byType[rt].out" :key="r.id" class="conn-row" @click="$emit('select', r.toId)">
                    <span class="conn-badge" :class="nodeById(r.toId)?.type?.toLowerCase()">{{ nodeById(r.toId)?.type?.[0] }}</span>
                    <span class="conn-name">{{ nodeById(r.toId)?.name }}</span>
                    <span class="conn-rel">→ {{ r.relationType }}</span>
                  </div>
                </div>
              </div>
              <div v-if="byType[rt].in.length" style="margin-top: 8px">
                <div class="rel-dir-label">others → this node</div>
                <div class="conn-list">
                  <div v-for="r in byType[rt].in" :key="r.id" class="conn-row" @click="$emit('select', r.fromId)">
                    <span class="conn-badge" :class="nodeById(r.fromId)?.type?.toLowerCase()">{{ nodeById(r.fromId)?.type?.[0] }}</span>
                    <span class="conn-name">{{ nodeById(r.fromId)?.name }}</span>
                    <span class="conn-rel">← {{ r.relationType }}</span>
                  </div>
                </div>
              </div>
            </div>
          </template>
        </template>
        <div v-if="!allRels.length" class="empty-state">No connections yet.</div>
      </template>
    </div>

    <!-- Action bar -->
    <div class="action-bar">
      <button class="act-btn" @click="$emit('edit')">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M16 3.5 20.5 8 8 20.5l-5 1 1-5z"/></svg>
        Edit
      </button>
      <button class="act-btn" @click="$emit('addRelation')">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7L11.5 7"/><path d="M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7L12.5 17"/>
        </svg>
        Relate
      </button>
      <button class="act-btn danger" @click="$emit('delete')">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13"/></svg>
      </button>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { ref, computed, reactive, watch } from 'vue'
import { h } from 'vue'
import type { NodeDto, EdgeDto } from '@/api/catalog'
import { useHealthStore } from '@/stores/health'

const props = defineProps<{
  node: NodeDto
  nodes: NodeDto[]
  edges: EdgeDto[]
}>()

const emit = defineEmits<{
  close: []
  select: [id: string]
  edit: []
  delete: []
  addRelation: []
  patch: [id: string, data: Partial<NodeDto>]
}>()

const tab = ref('overview')

// SVG icon components
const SvcIcon = () => h('svg', { width: 16, height: 16, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '1.7', 'stroke-linecap': 'round', 'stroke-linejoin': 'round' },
  [h('circle', { cx: 12, cy: 12, r: 3 }), h('path', { d: 'M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2 2M16.4 16.4l2 2M5.6 18.4l2-2M16.4 7.6l2-2' })])
const SrvIcon = () => h('svg', { width: 16, height: 16, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '1.7', 'stroke-linecap': 'round', 'stroke-linejoin': 'round' },
  [h('rect', { x: 3, y: 4, width: 18, height: 7, rx: '1.5' }), h('rect', { x: 3, y: 13, width: 18, height: 7, rx: '1.5' }), h('circle', { cx: 7, cy: '7.5', r: '.7', fill: 'currentColor' }), h('circle', { cx: 7, cy: '16.5', r: '.7', fill: 'currentColor' })])
const DbIcon = () => h('svg', { width: 16, height: 16, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '1.7', 'stroke-linecap': 'round', 'stroke-linejoin': 'round' },
  [h('ellipse', { cx: 12, cy: 5, rx: 8, ry: '2.5' }), h('path', { d: 'M4 5v7c0 1.4 3.6 2.5 8 2.5s8-1.1 8-2.5V5' }), h('path', { d: 'M4 12v7c0 1.4 3.6 2.5 8 2.5s8-1.1 8-2.5v-7' })])

const healthStore = useHealthStore()

const TABS = [
  { id: 'overview', label: 'Overview' },
  { id: 'connections', label: 'Connections' },
  { id: 'monitor', label: 'Monitor' },
]

const nodeStatus      = computed(() => healthStore.statusOf(props.node.id))
const nodeConfig      = computed(() => healthStore.configOf(props.node.id))
const nodeDispStatus  = computed(() => healthStore.displayStatus(props.node.id))

const monitorForm = reactive({ checkTarget: '', intervalSeconds: 30, retryCount: 3 })
const monitorSubmitting = ref(false)
const monitorError = ref<string | null>(null)

watch(() => props.node.id, () => {
  monitorError.value = null
  const cfg = nodeConfig.value
  monitorForm.checkTarget    = cfg?.checkTarget    ?? ''
  monitorForm.intervalSeconds = cfg?.intervalSeconds ?? 30
  monitorForm.retryCount     = cfg?.retryCount     ?? 3
}, { immediate: true })

watch(nodeConfig, (cfg) => {
  if (cfg) {
    monitorForm.checkTarget    = cfg.checkTarget
    monitorForm.intervalSeconds = cfg.intervalSeconds
    monitorForm.retryCount     = cfg.retryCount
  }
})

function formatAge(iso: string): string {
  const s = Math.floor((Date.now() - new Date(iso).getTime()) / 1000)
  if (s < 60)   return `${s}s ago`
  if (s < 3600) return `${Math.floor(s / 60)}m ago`
  return `${Math.floor(s / 3600)}h ago`
}

async function saveMonitor() {
  monitorSubmitting.value = true
  monitorError.value = null
  try {
    await healthStore.setConfig(props.node.id, {
      checkType:       props.node.type === 'Server' ? 'ping' : 'http',
      checkTarget:     monitorForm.checkTarget.trim(),
      intervalSeconds: monitorForm.intervalSeconds,
      retryCount:      monitorForm.retryCount,
    })
  } catch (e: any) {
    monitorError.value = e?.response?.data ?? 'Failed to save'
  } finally {
    monitorSubmitting.value = false
  }
}

async function removeMonitor() {
  monitorSubmitting.value = true
  monitorError.value = null
  try {
    await healthStore.deleteConfig(props.node.id)
    monitorForm.checkTarget    = ''
    monitorForm.intervalSeconds = 30
    monitorForm.retryCount     = 3
  } catch (e: any) {
    monitorError.value = e?.response?.data ?? 'Failed to remove'
  } finally {
    monitorSubmitting.value = false
  }
}

const DB_TYPES = [
  'Oracle','MySQL','Microsoft SQL Server','PostgreSQL','MongoDB','Redis','SQLite',
  'Elasticsearch','Snowflake','Amazon DynamoDB','Apache Cassandra','MariaDB',
  'Google BigQuery','Splunk','Firebase Realtime Database','Neo4j','Couchbase',
  'Teradata','IBM Db2','InfluxDB','Trino','CockroachDB','YugabyteDB','PlanetScale',
  'Neon','SingleStore','Yellowbrick','Actian Ingres','SAP HANA','Firebird','H2',
  'HSQLDB','DuckDB','Vertica','Amazon DocumentDB','RavenDB','ArangoDB','FerretDB',
  'Memcached','Hazelcast','Apache Ignite','Dragonfly','KeyDB','HBase','ScyllaDB',
  'Google Bigtable','TimescaleDB','QuestDB','Prometheus','kdb+','TDengine',
  'VictoriaMetrics','OpenSearch','Apache Solr','Typesense','Meilisearch',
  'Manticore Search','Amazon Neptune','TigerGraph','JanusGraph','Dgraph','FalkorDB',
  'Pinecone','Weaviate','Qdrant','Chroma','Milvus','pgvector','Apache Druid',
  'ClickHouse','StarRocks','Databricks','Azure Synapse','Amazon Redshift','Greenplum',
  'LevelDB','RocksDB','LMDB','Berkeley DB','Other',
] as const

const allRels = computed(() => {
  const out = props.edges.filter(e => e.fromId === props.node.id).map(e => ({ ...e, dir: 'out' as const }))
  const ins = props.edges.filter(e => e.toId   === props.node.id).map(e => ({ ...e, dir: 'in'  as const }))
  return [...out, ...ins]
})

const byType = computed(() => {
  const map: Record<string, { in: typeof allRels.value; out: typeof allRels.value }> = {
    RUNS_ON:  { in: [], out: [] },
    REQUIRES: { in: [], out: [] },
  }
  allRels.value.forEach(r => map[r.relationType]?.[r.dir]?.push(r))
  return map
})

function nodeById(id: string) { return props.nodes.find(n => n.id === id) }
function otherNode(rel: (typeof allRels.value)[0]) {
  return nodeById(rel.dir === 'out' ? rel.toId : rel.fromId)
}
function patch(key: string, value: string) {
  emit('patch', props.node.id, { [key]: value } as Partial<NodeDto>)
}
</script>

<style scoped>
.inspector {
  width: 340px; flex-shrink: 0; background: var(--c-panel);
  border-left: 1px solid var(--c-divider);
  display: flex; flex-direction: column; overflow: hidden;
}
.insp-header { padding: 14px 16px 12px; border-bottom: 1px solid var(--c-divider); }
.insp-title-row { display: flex; align-items: flex-start; gap: 10px; }
.node-icon {
  width: 32px; height: 32px; border-radius: 8px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  color: #fff;
}
.node-icon.service  { background: var(--c-service); }
.node-icon.server   { background: var(--c-server); }
.node-icon.database { background: var(--c-database); color: var(--c-text); }
.insp-title-text { flex: 1; min-width: 0; }
.insp-type { font-size: 10.5px; font-weight: 600; color: var(--c-muted); text-transform: uppercase; letter-spacing: .08em; }
.insp-name { font-size: 17px; font-weight: 600; color: var(--c-text); letter-spacing: -0.01em; margin-top: 2px; font-family: var(--font-mono); }
.insp-desc { font-size: 12.5px; color: var(--c-text-2); margin-top: 4px; line-height: 1.5; }
.close-btn { border: 0; background: transparent; cursor: pointer; color: var(--c-muted); padding: 4px; display: flex; border-radius: 6px; flex-shrink: 0; }
.close-btn:hover { color: var(--c-text); background: var(--c-panel-2); }
.tabs {
  display: flex; padding: 0 16px; border-bottom: 1px solid var(--c-divider); gap: 14px;
}
.tab-btn {
  position: relative; padding: 10px 0; border: 0; background: transparent; cursor: pointer;
  font-size: 12px; font-weight: 500; color: var(--c-muted); text-transform: capitalize; display: flex; align-items: center; gap: 5px;
}
.tab-btn.active { font-weight: 600; color: var(--c-text); }
.tab-btn.active::after {
  content: ''; position: absolute; left: 0; right: 0; bottom: -1px; height: 2px;
  background: var(--c-accent); border-radius: 2px;
}
.tab-count { font-size: 10px; color: var(--c-muted); font-family: var(--font-mono); font-weight: 500; }
.insp-body { flex: 1; overflow-y: auto; padding: 14px 16px; display: flex; flex-direction: column; gap: 16px; }
.section { display: flex; flex-direction: column; gap: 8px; }
.section-label { font-size: 10px; font-weight: 600; color: var(--c-muted); text-transform: uppercase; letter-spacing: .1em; }
.readonly-rows { display: flex; flex-direction: column; gap: 2px; }
.readonly-row {
  display: flex; align-items: center; justify-content: space-between;
  padding: 6px 8px; font-size: 12px; background: var(--c-panel-2);
  border: 1px solid var(--c-divider); border-radius: 6px;
}
.rr-key { color: var(--c-muted); font-family: var(--font-mono); }
.rr-val { color: var(--c-text-2); font-family: var(--font-mono); font-size: 11.5px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 60%; text-align: right; }
.editable-rows { display: flex; flex-direction: column; }
.editable-row {
  display: grid; grid-template-columns: 80px 1fr; gap: 8px; align-items: flex-start;
  padding: 6px 0; border-bottom: 1px dashed var(--c-divider);
}
.er-label { font-size: 11.5px; color: var(--c-muted); padding-top: 6px; }
.er-input {
  width: 100%; padding: 5px 7px; font-size: 12px; font-family: var(--font-mono);
  color: var(--c-text); background: var(--c-panel); border: 1px solid transparent;
  border-radius: 5px; outline: none; transition: border-color .12s, background .12s;
}
.er-input:hover { background: var(--c-panel-2); border-color: var(--c-divider); }
.er-input:focus { background: var(--c-panel); border-color: var(--c-accent); }
.er-textarea { resize: vertical; min-height: 44px; line-height: 1.4; }
.er-select { cursor: pointer; }
.conn-list { display: flex; flex-direction: column; gap: 4px; }
.conn-row {
  display: flex; align-items: center; gap: 8px; padding: 6px 8px; border-radius: 6px;
  cursor: pointer; border: 1px solid var(--c-divider); background: var(--c-panel);
  transition: background .1s;
}
.conn-row:hover { background: var(--c-panel-2); }
.conn-badge {
  width: 16px; height: 16px; border-radius: 3px; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 700; font-family: var(--font-mono); color: #fff;
}
.conn-badge.service  { background: var(--c-service); }
.conn-badge.server   { background: var(--c-server); }
.conn-badge.database { background: var(--c-database); color: var(--c-text); }
.conn-name { font-size: 12.5px; color: var(--c-text); flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.conn-rel { font-size: 10px; color: var(--c-muted); font-family: var(--font-mono); flex-shrink: 0; }
.rel-type-header { display: flex; align-items: center; gap: 6px; margin-bottom: 6px; }
.rel-line { width: 16px; height: 2px; border-radius: 2px; display: inline-block; }
.rel-line.solid { background: var(--c-runs-on); }
.rel-line.dashed { height: 0; border-top: 2px dashed var(--c-requires); background: transparent; }
.rel-type-label { font-size: 10.5px; font-weight: 600; color: var(--c-text-2); letter-spacing: .06em; font-family: var(--font-mono); }
.rel-dir-label { font-size: 10.5px; color: var(--c-muted); margin-bottom: 4px; }
.empty-state { padding: 16px; font-size: 12px; color: var(--c-muted); text-align: center; }
.action-bar {
  display: flex; gap: 6px; padding: 12px; border-top: 1px solid var(--c-divider);
}
.act-btn {
  flex: 1; display: flex; align-items: center; justify-content: center; gap: 5px;
  padding: 7px 12px; border: 1px solid var(--c-divider); border-radius: 7px;
  background: var(--c-panel); color: var(--c-text); font-size: 12px; font-weight: 500; cursor: pointer;
  transition: background .1s;
}
.act-btn:hover { background: var(--c-panel-2); }
.act-btn.danger { flex: none; padding: 7px 12px; border-color: var(--c-divider); color: var(--c-err); }
.act-btn.danger:hover { background: color-mix(in oklab, var(--c-err) 10%, transparent); border-color: var(--c-err); }
.tab-health-dot { width: 5px; height: 5px; border-radius: 999px; display: inline-block; flex-shrink: 0; }
.tab-health-dot.available    { background: var(--c-ok); }
.tab-health-dot.compromised  { background: var(--c-warn); }
.tab-health-dot.unavailable  { background: var(--c-err); }
.section-label-row { display: flex; align-items: center; justify-content: space-between; }
.check-type-badge {
  font-size: 9.5px; font-weight: 600; font-family: var(--font-mono);
  padding: 2px 6px; border-radius: 4px; letter-spacing: .04em; text-transform: uppercase;
}
.check-type-badge.http { background: color-mix(in oklab, var(--c-accent) 12%, transparent); color: var(--c-accent); }
.check-type-badge.ping { background: color-mix(in oklab, var(--c-server) 12%, transparent); color: var(--c-text-2); }
.health-status-row {
  display: flex; align-items: center; gap: 8px; padding: 8px 10px;
  border-radius: 7px; background: var(--c-panel-2); border: 1px solid var(--c-divider);
}
.health-status-row.available    { border-color: color-mix(in oklab, var(--c-ok)   35%, var(--c-divider)); }
.health-status-row.compromised  { border-color: color-mix(in oklab, var(--c-warn) 35%, var(--c-divider)); }
.health-status-row.unavailable  { border-color: color-mix(in oklab, var(--c-err)  35%, var(--c-divider)); }
.health-dot { width: 7px; height: 7px; border-radius: 999px; flex-shrink: 0; box-shadow: 0 0 0 1.5px var(--c-muted); }
.health-dot.available   { background: var(--c-ok); }
.health-dot.compromised { background: var(--c-warn); animation: health-blink-warn 1.2s ease-in-out infinite; }
.health-dot.unavailable { background: var(--c-err);  animation: health-blink-err  1.2s ease-in-out infinite; }
.health-label { font-size: 12.5px; font-weight: 600; color: var(--c-text); flex: 1; }
.health-time  { font-size: 11px; color: var(--c-muted); font-family: var(--font-mono); }
.health-err-text { font-size: 11px; color: var(--c-err); margin-top: 6px; font-family: var(--font-mono); }
.monitor-btns { display: flex; gap: 8px; }
.btn-save {
  flex: 1; padding: 8px 14px; border: 0; border-radius: 7px; cursor: pointer;
  background: var(--c-accent); color: #fff; font-size: 12px; font-weight: 500;
  transition: background .1s;
}
.btn-save:hover:not(:disabled) { background: color-mix(in oklab, var(--c-accent) 85%, black); }
.btn-save:disabled { opacity: .45; cursor: not-allowed; }
.btn-remove {
  padding: 8px 14px; border: 1px solid var(--c-err); border-radius: 7px; cursor: pointer;
  background: transparent; color: var(--c-err); font-size: 12px; font-weight: 500;
  transition: background .1s;
}
.btn-remove:hover:not(:disabled) { background: color-mix(in oklab, var(--c-err) 10%, transparent); }
.btn-remove:disabled { opacity: .45; cursor: not-allowed; }
</style>
