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
import { ref, computed } from 'vue'
import { h } from 'vue'
import type { NodeDto, EdgeDto } from '@/api/catalog'

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

const TABS = [
  { id: 'overview', label: 'Overview' },
  { id: 'connections', label: 'Connections' },
]

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
</style>
