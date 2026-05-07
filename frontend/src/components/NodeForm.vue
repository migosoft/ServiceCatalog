<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-head">
        <span class="modal-title">{{ node ? 'Edit node' : 'New node' }}</span>
        <button class="modal-close" @click="$emit('close')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M6 6l12 12M18 6 6 18"/></svg>
        </button>
      </div>

      <div class="modal-body">
        <form @submit.prevent="submit">
          <!-- Type picker -->
          <div class="field">
            <label class="field-label">Type</label>
            <div class="type-cards">
              <button
                v-for="t in TYPES"
                :key="t"
                type="button"
                :disabled="!!node"
                :class="['type-card', { active: form.type === t }]"
                @click="form.type = t"
              >
                <span class="type-dot" :style="{ background: `var(--c-${t.toLowerCase()})` }" />
                {{ t }}
              </button>
            </div>
          </div>

          <!-- Name -->
          <div class="field">
            <label class="field-label">Name</label>
            <input v-model="form.name" class="field-input" placeholder="my-service" required />
          </div>

          <!-- OS (Server only) -->
          <div v-if="form.type === 'Server'" class="field">
            <label class="field-label">Operating system</label>
            <select v-model="form.os" class="field-input">
              <option value="Linux">Linux</option>
              <option value="Windows">Windows</option>
            </select>
          </div>

          <!-- Owner -->
          <div class="field">
            <label class="field-label">Owner</label>
            <input v-model="form.owner" class="field-input" placeholder="team or person responsible" />
          </div>

          <!-- Address -->
          <div class="field">
            <label class="field-label">Address</label>
            <input v-model="form.address" class="field-input" placeholder="hostname, IP or URL" />
          </div>

          <!-- DB Type (Database only) -->
          <div v-if="form.type === 'Database'" class="field">
            <label class="field-label">Type</label>
            <select v-model="form.dbType" class="field-input">
              <option value="">— select —</option>
              <option v-for="opt in DB_TYPES" :key="opt" :value="opt">{{ opt }}</option>
            </select>
          </div>

          <!-- Description -->
          <div class="field">
            <label class="field-label">Description</label>
            <textarea
              v-model="form.description"
              class="field-input"
              placeholder="What does this do?"
              rows="3"
            />
          </div>

          <p v-if="error" class="error-msg">{{ error }}</p>

          <div class="field-actions">
            <button type="button" class="btn-secondary" @click="$emit('close')">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="submitting">
              {{ node ? 'Save changes' : 'Create node' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useCatalogStore } from '@/stores/catalog'
import type { NodeDto } from '@/api/catalog'

const props = defineProps<{ node?: NodeDto }>()
const emit = defineEmits<{ close: []; saved: [node: NodeDto] }>()

const store = useCatalogStore()
const submitting = ref(false)
const error = ref<string | null>(null)

const TYPES = ['Service', 'Server', 'Database'] as const

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

const form = reactive({
  type: props.node?.type ?? 'Service',
  name: props.node?.name ?? '',
  description: props.node?.description ?? '',
  os:      props.node?.properties?.['os']      ?? 'Linux',
  owner:   props.node?.properties?.['owner']   ?? '',
  address: props.node?.properties?.['address'] ?? '',
  dbType:  props.node?.properties?.['db_type'] ?? '',
})

async function submit() {
  submitting.value = true
  error.value = null
  try {
    let saved: NodeDto
    const os      = form.type === 'Server'   ? form.os              : undefined
    const owner   = form.owner.trim()   || undefined
    const address = form.address.trim() || undefined
    const dbType  = form.type === 'Database' ? form.dbType.trim() || undefined : undefined
    if (props.node) {
      saved = await store.updateNode(props.node.id, { name: form.name, description: form.description, operatingSystem: os, owner, address, dbType })
    } else {
      saved = await store.createNode({ type: form.type, name: form.name, description: form.description, operatingSystem: os, owner, address, dbType })
    }
    emit('saved', saved)
    emit('close')
  } catch (e: any) {
    error.value = e?.response?.data ?? 'An error occurred'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; z-index: 100;
  background: rgba(20,17,12,.42); backdrop-filter: blur(4px);
  display: flex; align-items: center; justify-content: center; padding: 24px;
}
.modal {
  width: 440px; background: var(--c-panel);
  border: 1px solid var(--c-divider); border-radius: 14px;
  box-shadow: var(--shadow-pop);
}
.modal-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 14px 18px; border-bottom: 1px solid var(--c-divider);
}
.modal-title { font-size: 14px; font-weight: 600; color: var(--c-text); }
.modal-close {
  border: 0; background: transparent; cursor: pointer; padding: 4px;
  color: var(--c-muted); display: flex; border-radius: 6px;
}
.modal-close:hover { color: var(--c-text); background: var(--c-panel-2); }
.modal-body { padding: 18px; display: flex; flex-direction: column; gap: 14px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field-label { font-size: 11px; font-weight: 600; color: var(--c-muted); text-transform: uppercase; letter-spacing: .08em; }
.field-input {
  width: 100%; padding: 8px 10px; font-size: 13px;
  background: var(--c-input); border: 1px solid var(--c-divider);
  border-radius: 7px; color: var(--c-text); outline: none;
  transition: border-color .12s;
}
.field-input:focus { border-color: var(--c-accent); }
textarea.field-input { resize: vertical; line-height: 1.5; }
.type-cards { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px; }
.type-card {
  display: flex; align-items: center; justify-content: center; gap: 6px;
  padding: 8px; border-radius: 7px; cursor: pointer; font-size: 12px; font-weight: 500;
  border: 1px solid var(--c-divider); background: var(--c-panel); color: var(--c-text);
  transition: background .1s, border-color .1s;
}
.type-card:hover:not(:disabled) { background: var(--c-panel-2); }
.type-card.active { background: var(--c-panel-2); border-color: var(--c-text); }
.type-card:disabled { opacity: .5; cursor: not-allowed; }
.type-dot { width: 8px; height: 8px; border-radius: 2px; display: inline-block; }
.field-actions { display: flex; gap: 8px; justify-content: flex-end; margin-top: 4px; }
.btn-primary {
  padding: 8px 18px; border: 0; border-radius: 7px; cursor: pointer;
  background: var(--c-accent); color: #fff; font-size: 13px; font-weight: 500;
}
.btn-primary:hover { background: color-mix(in oklab, var(--c-accent) 85%, black); }
.btn-primary:disabled { opacity: .5; cursor: not-allowed; }
.btn-secondary {
  padding: 8px 18px; border: 1px solid var(--c-divider); border-radius: 7px; cursor: pointer;
  background: transparent; color: var(--c-text-2); font-size: 13px; font-weight: 500;
}
.btn-secondary:hover { background: var(--c-panel-2); }
.error-msg { font-size: 12px; color: var(--c-err); }
</style>
