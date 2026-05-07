<template>
  <div class="grid-view">
    <div class="grid-inner">
      <template v-for="type in TYPE_ORDER" :key="type">
      <section v-if="grouped[type].length">
        <div class="group-header">
          <span class="type-chip" :style="{ background: `var(--c-${type.toLowerCase()})` }" />
          <h3 class="group-title">{{ type }}</h3>
          <span class="group-count">{{ grouped[type].length }}</span>
        </div>
        <div class="card-grid">
          <div
            v-for="n in grouped[type]"
            :key="n.id"
            :class="['card', { selected: selectedId === n.id }]"
            @click="$emit('select', n.id)"
          >
            <div class="card-head">
              <span class="card-icon" :class="n.type.toLowerCase()">
                <SvcIcon v-if="n.type === 'Service'" />
                <SrvIcon v-else-if="n.type === 'Server'" />
                <DbIcon  v-else />
              </span>
              <span class="card-type">{{ n.type }}</span>
              <div class="spacer" />
              <span class="status-dot ok" />
            </div>
            <div class="card-name">{{ n.name }}</div>
            <div v-if="n.description" class="card-desc">{{ n.description }}</div>
            <div class="card-meta">
              <span v-if="n.properties?.['os']">{{ n.properties['os'] }}</span>
              <span v-if="n.properties?.['owner']" class="owner">
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/>
                </svg>
                {{ n.properties['owner'] }}
              </span>
              <div class="spacer" />
              <span class="degree">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7L11.5 7"/><path d="M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7L12.5 17"/>
                </svg>
                {{ degree[n.id] ?? 0 }}
              </span>
            </div>
          </div>
        </div>
      </section>
      </template>
      <div v-if="totalNodes === 0" class="empty">No nodes yet. Add one with the sidebar.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, h } from 'vue'
import type { NodeDto, EdgeDto } from '@/api/catalog'

const props = defineProps<{
  nodes: NodeDto[]
  edges: EdgeDto[]
  selectedId: string | null
}>()

defineEmits<{ select: [id: string] }>()

const TYPE_ORDER = ['Server', 'Database', 'Service'] as const

// Inline SVG icons
const SvcIcon = () => h('svg', { width: 13, height: 13, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '1.7', 'stroke-linecap': 'round', 'stroke-linejoin': 'round' },
  [h('circle', { cx: 12, cy: 12, r: 3 }), h('path', { d: 'M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2 2M16.4 16.4l2 2M5.6 18.4l2-2M16.4 7.6l2-2' })])
const SrvIcon = () => h('svg', { width: 13, height: 13, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '1.7', 'stroke-linecap': 'round', 'stroke-linejoin': 'round' },
  [h('rect', { x: 3, y: 4, width: 18, height: 7, rx: '1.5' }), h('rect', { x: 3, y: 13, width: 18, height: 7, rx: '1.5' }), h('circle', { cx: 7, cy: '7.5', r: '.7', fill: 'currentColor' }), h('circle', { cx: 7, cy: '16.5', r: '.7', fill: 'currentColor' })])
const DbIcon = () => h('svg', { width: 13, height: 13, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '1.7', 'stroke-linecap': 'round', 'stroke-linejoin': 'round' },
  [h('ellipse', { cx: 12, cy: 5, rx: 8, ry: '2.5' }), h('path', { d: 'M4 5v7c0 1.4 3.6 2.5 8 2.5s8-1.1 8-2.5V5' }), h('path', { d: 'M4 12v7c0 1.4 3.6 2.5 8 2.5s8-1.1 8-2.5v-7' })])

const grouped = computed(() => {
  const g: Record<string, NodeDto[]> = { Server: [], Database: [], Service: [] }
  props.nodes.forEach(n => { (g[n.type] ??= []).push(n) })
  Object.values(g).forEach(arr => arr.sort((a, b) => a.name.localeCompare(b.name)))
  return g
})

const degree = computed(() => {
  const d: Record<string, number> = {}
  props.edges.forEach(e => {
    d[e.fromId] = (d[e.fromId] ?? 0) + 1
    d[e.toId]   = (d[e.toId]   ?? 0) + 1
  })
  return d
})

const totalNodes = computed(() => props.nodes.length)
</script>

<style scoped>
.grid-view { position: absolute; inset: 0; overflow: auto; padding: 24px; background: var(--c-page); }
.grid-inner { max-width: 1400px; display: flex; flex-direction: column; gap: 24px; }
.group-header { display: flex; align-items: baseline; gap: 8px; margin-bottom: 10px; }
.type-chip { width: 10px; height: 10px; border-radius: 3px; display: inline-block; flex-shrink: 0; }
.group-title { margin: 0; font-size: 12px; font-weight: 600; color: var(--c-text); text-transform: uppercase; letter-spacing: .1em; font-family: var(--font-mono); }
.group-count { font-size: 11px; color: var(--c-muted); font-family: var(--font-mono); }
.card-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 12px; }
.card {
  background: var(--c-panel); border: 1px solid var(--c-divider); border-radius: 12px; padding: 14px;
  cursor: pointer; box-shadow: var(--shadow-1); transition: border-color .12s, box-shadow .12s;
}
.card:hover:not(.selected) { border-color: var(--c-divider-strong); }
.card.selected { border-color: var(--c-accent); box-shadow: var(--shadow-2); }
.card-head { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
.card-icon { width: 24px; height: 24px; border-radius: 6px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.card-icon.service  { background: var(--c-service); color: #fff; }
.card-icon.server   { background: var(--c-server);  color: #fff; }
.card-icon.database { background: var(--c-database); color: var(--c-text); }
.card-type { font-size: 10.5px; font-weight: 600; color: var(--c-muted); text-transform: uppercase; letter-spacing: .08em; }
.spacer { flex: 1; }
.status-dot { width: 6px; height: 6px; border-radius: 999px; }
.status-dot.ok { background: var(--c-ok); }
.card-name { font-size: 14px; font-weight: 600; color: var(--c-text); font-family: var(--font-mono); margin-bottom: 4px; }
.card-desc {
  font-size: 12px; color: var(--c-text-2); line-height: 1.5; margin-bottom: 10px;
  display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
}
.card-meta { display: flex; align-items: center; gap: 8px; font-size: 11px; color: var(--c-muted); font-family: var(--font-mono); margin-top: 8px; }
.degree { display: inline-flex; align-items: center; gap: 3px; }
.owner  { display: inline-flex; align-items: center; gap: 3px; max-width: 120px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.empty { padding: 48px 24px; text-align: center; color: var(--c-muted); font-size: 13px; }
</style>
