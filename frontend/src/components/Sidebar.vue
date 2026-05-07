<template>
  <aside class="sidebar" :style="{ width: sidebarWidth + 'px' }">
    <!-- Search -->
    <div class="search-section">
      <div class="search-wrap">
        <svg class="search-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>
        </svg>
        <input
          :value="search"
          @input="$emit('update:search', ($event.target as HTMLInputElement).value)"
          placeholder="Search services, servers, databases…"
          class="search-input"
        />
        <button v-if="search" class="search-clear" @click="$emit('update:search', '')">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
            <path d="M6 6l12 12M18 6 6 18"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- Type filter chips -->
    <div class="filter-section">
      <div class="section-label">Filter</div>
      <div class="type-chips">
        <button
          v-for="t in TYPES"
          :key="t"
          :class="['type-chip', { active: filterTypes.size === 0 || filterTypes.has(t) }]"
          @click="toggleType(t)"
        >
          <div class="chip-header">
            <span class="type-dot" :style="{ background: `var(--c-${t.toLowerCase()})` }" />
            <span class="chip-abbr">{{ t.slice(0, 3) }}</span>
          </div>
          <div class="chip-count">{{ counts[t] ?? 0 }}</div>
        </button>
      </div>
    </div>

    <!-- Add buttons -->
    <div class="add-section">
      <button class="btn-primary" @click="$emit('addNode')">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M12 5v14M5 12h14"/></svg>
        New node
      </button>
      <button class="btn-secondary" @click="$emit('addEdge')">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7L11.5 7"/><path d="M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7L12.5 17"/>
        </svg>
        Relation
      </button>
    </div>

    <!-- Node directory -->
    <div class="directory">
      <template v-for="t in TYPES" :key="t">
        <div v-if="grouped[t].length" class="type-group">
          <div class="group-header">
            <span class="type-dot" :style="{ background: `var(--c-${t.toLowerCase()})` }" />
            <span class="group-label">{{ t }}s</span>
            <span class="group-count">{{ grouped[t].length }}</span>
          </div>
          <div
            v-for="n in grouped[t]"
            :key="n.id"
            :class="['node-item', { selected: selectedId === n.id }]"
            @click="$emit('select', n.id)"
          >
            <span class="node-badge" :class="n.type.toLowerCase()">{{ n.type[0] }}</span>
            <span class="node-name">{{ n.name }}</span>
          </div>
        </div>
      </template>
      <div v-if="totalVisible === 0" class="empty-state">No matches.</div>
    </div>

    <p v-if="error" class="error-msg">{{ error }}</p>
  </aside>

  <!-- Resize handle -->
  <div class="resize-handle" :class="{ dragging: resizing }" @mousedown="startResize" />
</template>

<script setup lang="ts">
import { computed, ref, onUnmounted } from 'vue'
import type { NodeDto } from '@/api/catalog'

const props = defineProps<{
  nodes: NodeDto[]
  filterTypes: Set<string>
  search: string
  selectedId: string | null
  error?: string | null
}>()

const emit = defineEmits<{
  'update:search': [v: string]
  'update:filterTypes': [v: Set<string>]
  select: [id: string]
  addNode: []
  addEdge: []
}>()

const TYPES = ['Service', 'Server', 'Database'] as const

const counts = computed(() => {
  const c: Record<string, number> = {}
  props.nodes.forEach(n => { c[n.type] = (c[n.type] ?? 0) + 1 })
  return c
})

const filtered = computed(() => props.nodes.filter(n => {
  if (props.filterTypes.size && !props.filterTypes.has(n.type)) return false
  if (props.search) {
    const q = props.search.toLowerCase()
    if (!n.name.toLowerCase().includes(q) && !(n.description ?? '').toLowerCase().includes(q)) return false
  }
  return true
}))

const grouped = computed(() => {
  const g: Record<string, NodeDto[]> = { Service: [], Server: [], Database: [] }
  filtered.value.forEach(n => g[n.type]?.push(n))
  return g
})

const totalVisible = computed(() => filtered.value.length)

function toggleType(t: string) {
  const next = new Set(props.filterTypes)
  next.has(t) ? next.delete(t) : next.add(t)
  emit('update:filterTypes', next)
}

// Resizable width
const sidebarWidth = ref(280)
const resizing = ref(false)
let _startX = 0, _startW = 0

function startResize(e: MouseEvent) {
  resizing.value = true
  _startX = e.clientX
  _startW = sidebarWidth.value
  document.addEventListener('mousemove', doResize)
  document.addEventListener('mouseup', stopResize)
  e.preventDefault()
}
function doResize(e: MouseEvent) {
  sidebarWidth.value = Math.max(220, Math.min(480, _startW + (e.clientX - _startX)))
}
function stopResize() {
  resizing.value = false
  document.removeEventListener('mousemove', doResize)
  document.removeEventListener('mouseup', stopResize)
}
onUnmounted(() => {
  document.removeEventListener('mousemove', doResize)
  document.removeEventListener('mouseup', stopResize)
})
</script>

<style scoped>
.sidebar {
  flex-shrink: 0; background: var(--c-panel);
  display: flex; flex-direction: column; overflow: hidden;
}
.search-section { padding: 12px 14px; border-bottom: 1px solid var(--c-divider); }
.search-wrap { position: relative; }
.search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); color: var(--c-muted); pointer-events: none; }
.search-input {
  width: 100%; padding: 8px 32px; font-size: 13px;
  background: var(--c-input); border: 1px solid var(--c-divider);
  border-radius: 8px; color: var(--c-text); outline: none;
  transition: border-color .12s;
}
.search-input:focus { border-color: var(--c-accent); }
.search-clear {
  position: absolute; right: 6px; top: 50%; transform: translateY(-50%);
  border: 0; background: transparent; cursor: pointer; color: var(--c-muted);
  padding: 4px; display: flex; border-radius: 4px;
}
.search-clear:hover { color: var(--c-text); }
.filter-section { padding: 12px 14px; border-bottom: 1px solid var(--c-divider); }
.section-label { font-size: 10px; font-weight: 600; color: var(--c-muted); text-transform: uppercase; letter-spacing: .1em; margin-bottom: 8px; }
.type-chips { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px; }
.type-chip {
  display: flex; flex-direction: column; align-items: flex-start; gap: 4px;
  padding: 8px 10px; border: 1px solid var(--c-divider); border-radius: 8px;
  background: var(--c-panel); cursor: pointer; text-align: left;
  opacity: 0.5; transition: background .12s, opacity .12s;
}
.type-chip.active { background: var(--c-panel-2); opacity: 1; }
.chip-header { display: flex; align-items: center; gap: 5px; }
.type-dot { width: 7px; height: 7px; border-radius: 2px; display: inline-block; flex-shrink: 0; }
.chip-abbr { font-size: 10.5px; font-weight: 600; text-transform: uppercase; letter-spacing: .04em; color: var(--c-text-2); }
.chip-count { font-size: 18px; font-weight: 600; font-family: var(--font-mono); font-variant-numeric: tabular-nums; color: var(--c-text); }
.add-section { padding: 10px 14px; border-bottom: 1px solid var(--c-divider); display: flex; gap: 6px; }
.btn-primary {
  flex: 1; display: flex; align-items: center; justify-content: center; gap: 5px;
  padding: 7px 10px; border: 0; border-radius: 7px;
  background: var(--c-text); color: var(--c-panel);
  font-size: 12px; font-weight: 500; cursor: pointer;
}
.btn-primary:hover { background: var(--c-accent); }
.btn-secondary {
  flex: 1; display: flex; align-items: center; justify-content: center; gap: 5px;
  padding: 7px 10px; border: 1px solid var(--c-divider); border-radius: 7px;
  background: var(--c-panel); color: var(--c-text);
  font-size: 12px; font-weight: 500; cursor: pointer;
}
.btn-secondary:hover { background: var(--c-panel-2); }
.directory { flex: 1; overflow-y: auto; padding: 8px 8px 16px; }
.type-group { margin-bottom: 4px; }
.group-header {
  display: flex; align-items: center; gap: 6px; padding: 8px 10px 4px;
  font-size: 10px; font-weight: 600; text-transform: uppercase;
  letter-spacing: .08em; color: var(--c-muted);
}
.group-label { flex: 1; }
.group-count { font-family: var(--font-mono); font-weight: 500; opacity: .7; }
.node-item {
  display: flex; align-items: center; gap: 8px; padding: 6px 10px;
  border-radius: 6px; cursor: pointer; position: relative;
  transition: background .1s;
}
.node-item:hover:not(.selected) { background: var(--c-panel-2); }
.node-item.selected { background: var(--c-accent-soft); }
.node-item.selected::before {
  content: ''; position: absolute; left: 0; top: 6px; bottom: 6px;
  width: 2px; background: var(--c-accent); border-radius: 2px;
}
.node-badge {
  width: 16px; height: 16px; border-radius: 3px;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 700; font-family: var(--font-mono); flex-shrink: 0;
  color: #fff;
}
.node-badge.service { background: var(--c-service); }
.node-badge.server  { background: var(--c-server); }
.node-badge.database { background: var(--c-database); color: var(--c-text); }
.node-name { font-size: 13px; color: var(--c-text); flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.empty-state { padding: 24px; text-align: center; color: var(--c-muted); font-size: 12px; }
.error-msg { padding: 8px 14px; font-size: 12px; color: var(--c-err); }

/* Resize handle lives outside aside so it can be a sibling */
.resize-handle {
  width: 5px; flex-shrink: 0; cursor: col-resize;
  background: var(--c-divider); transition: background .15s;
}
.resize-handle:hover, .resize-handle.dragging { background: var(--c-accent); }
</style>
