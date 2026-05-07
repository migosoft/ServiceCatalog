<template>
  <div
    ref="container"
    class="graph-canvas"
    @wheel.prevent="onWheel"
    @mousedown="onCanvasDown"
    @contextmenu.prevent
  >
    <!-- Dot-grid background -->
    <svg class="bg-svg" width="100%" height="100%">
      <defs>
        <pattern id="gc-dots" width="32" height="32" patternUnits="userSpaceOnUse"
                 :patternTransform="`translate(${((vp.x % 32) + 32) % 32} ${((vp.y % 32) + 32) % 32})`">
          <circle cx="1" cy="1" r="0.65" fill="var(--c-divider-strong)" opacity="0.55" />
        </pattern>
      </defs>
      <rect width="100%" height="100%" fill="url(#gc-dots)" />
    </svg>

    <!-- Graph SVG -->
    <svg class="graph-svg">
      <defs>
        <marker id="gc-arr-runs" viewBox="0 0 10 10" refX="9" refY="5"
                markerWidth="7" markerHeight="7" orient="auto">
          <path d="M0 1.5L8.5 5L0 8.5z" fill="var(--c-runs-on)" />
        </marker>
        <marker id="gc-arr-req" viewBox="0 0 10 10" refX="9" refY="5"
                markerWidth="7" markerHeight="7" orient="auto">
          <path d="M0 1.5L8.5 5L0 8.5z" fill="var(--c-requires)" />
        </marker>
      </defs>

      <g :transform="`translate(${vp.x},${vp.y}) scale(${vp.k})`">
        <!-- Edges -->
        <template v-for="e in props.edges" :key="e.id">
          <g v-if="edgePath(e)"
             :style="{ opacity: isEdgeDimmed(e) ? 0.14 : 1, transition: 'opacity .2s' }">
            <!-- Wide transparent hit area for right-click -->
            <path :d="edgePath(e)!.d" fill="none" stroke="transparent" stroke-width="12"
                  @contextmenu.prevent.stop="onEdgeCtx($event, e)"
                  style="cursor: pointer" />
            <path :d="edgePath(e)!.d" fill="none"
                  :stroke="e.relationType === 'RUNS_ON' ? 'var(--c-runs-on)' : 'var(--c-requires)'"
                  stroke-width="1.5"
                  :stroke-dasharray="e.relationType === 'REQUIRES' ? '5 4' : 'none'"
                  :marker-end="e.relationType === 'RUNS_ON' ? 'url(#gc-arr-runs)' : 'url(#gc-arr-req)'"
                  style="pointer-events: none" />
            <!-- Label on hover -->
            <g v-if="showEdgeLabel(e)"
               :transform="`translate(${edgePath(e)!.mx},${edgePath(e)!.my})`"
               style="pointer-events: none">
              <rect x="-31" y="-9" width="62" height="18" rx="9"
                    fill="var(--c-panel)" stroke="var(--c-divider)" stroke-width="0.5" />
              <text x="0" y="3.5" text-anchor="middle" font-size="9.5"
                    font-family="var(--font-mono)" font-weight="500"
                    fill="var(--c-text-2)">{{ e.relationType }}</text>
            </g>
          </g>
        </template>

        <!-- Nodes -->
        <g v-for="n in props.nodes" :key="n.id"
           :transform="nodeTransform(n)"
           :style="{ cursor: draggingId === n.id ? 'grabbing' : 'grab', opacity: isNodeDimmed(n) ? 0.24 : 1, transition: 'opacity .2s' }"
           @mousedown.stop="onNodeDown($event, n)"
           @contextmenu.prevent.stop="onNodeCtx($event, n)"
           @mouseenter="hoverId = n.id"
           @mouseleave="hoverId = null">

          <!-- Animated highlight ring -->
          <rect v-if="isHighlighted(n) || clickedId === n.id"
                :x="-5" :y="-5"
                :width="nodeW(n) + 10" :height="NODE_H + 10"
                rx="14" fill="none"
                stroke="var(--c-accent)" stroke-width="2.2"
                stroke-dasharray="4 3" opacity="0.8">
            <animate attributeName="stroke-dashoffset" from="0" to="-14" dur="1s" repeatCount="indefinite" />
          </rect>

          <!-- Card background -->
          <rect :width="nodeW(n)" :height="NODE_H" rx="9"
                fill="var(--c-panel)"
                :stroke="hoverId === n.id ? 'var(--c-text)' : 'var(--c-divider-strong)'"
                :stroke-width="hoverId === n.id ? 1.3 : 1" />

          <!-- Icon badge -->
          <rect :x="BADGE_X" :y="(NODE_H - BADGE) / 2"
                :width="BADGE" :height="BADGE" rx="6"
                :fill="NODE_COLORS[n.type] ?? '#888'" />

          <!-- Service icon -->
          <g v-if="n.type === 'Service'"
             :transform="`translate(${BADGE_X + (BADGE - ICON) / 2},${(NODE_H - ICON) / 2})`"
             stroke="#fff" stroke-width="1.55" stroke-linecap="round" fill="none">
            <circle cx="8" cy="8" r="2.3" />
            <path d="M8 2.5v2M8 11.5v2M2.5 8h2M11.5 8h2M4 4l1.5 1.5M10.5 10.5l1.5 1.5M4 12l1.5-1.5M10.5 5.5l1.5-1.5" stroke-width="1.4" />
          </g>

          <!-- Server icon -->
          <g v-else-if="n.type === 'Server'"
             :transform="`translate(${BADGE_X + (BADGE - ICON) / 2},${(NODE_H - ICON) / 2})`"
             stroke="#fff" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" fill="none">
            <rect x="1" y="2" width="14" height="4.5" rx="1.2" />
            <rect x="1" y="8.5" width="14" height="4.5" rx="1.2" />
            <circle cx="3.7" cy="4.25" r="0.7" fill="#fff" stroke="none" />
            <circle cx="3.7" cy="10.75" r="0.7" fill="#fff" stroke="none" />
          </g>

          <!-- Database icon -->
          <g v-else
             :transform="`translate(${BADGE_X + (BADGE - ICON) / 2},${(NODE_H - ICON) / 2})`"
             :stroke="n.type === 'Database' ? 'var(--c-text)' : '#fff'"
             stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" fill="none">
            <ellipse cx="8" cy="4" rx="5.5" ry="1.8" />
            <path d="M2.5 4v4c0 1 2.5 1.8 5.5 1.8S13.5 9 13.5 8V4" />
            <path d="M2.5 8v3c0 1 2.5 1.8 5.5 1.8S13.5 12 13.5 11V8" />
          </g>

          <!-- Node name -->
          <text :x="BADGE_X + BADGE + 8" :y="NODE_H / 2 + 4.5"
                text-anchor="start" font-size="13" font-weight="600"
                fill="var(--c-text)"
                style="pointer-events:none;font-family:var(--font-sans)">
            {{ n.name }}
          </text>
        </g>
      </g>
    </svg>

    <!-- Zoom controls (top-right) -->
    <div class="zoom-controls">
      <button class="zoom-btn" title="Zoom in" @click="zoomIn">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="11" cy="11" r="7"/><path d="M11 8v6M8 11h6M21 21l-4-4"/>
        </svg>
      </button>
      <div class="zoom-sep" />
      <button class="zoom-btn" title="Zoom out" @click="zoomOut">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="11" cy="11" r="7"/><path d="M8 11h6M21 21l-4-4"/>
        </svg>
      </button>
      <div class="zoom-sep" />
      <button class="zoom-btn" title="Fit to view" @click="fitView">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <path d="M4 9V4h5M20 9V4h-5M4 15v5h5M20 15v5h-5"/>
        </svg>
      </button>
    </div>

    <!-- Zoom % indicator + reset (bottom-right) -->
    <div class="zoom-pct">
      <span>{{ Math.round(vp.k * 100) }}%</span>
      <button class="zoom-reset-btn" title="Reset zoom to 100%" @click="resetZoom">
        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 12a9 9 0 1 0 3-6.7L3 8"/><path d="M3 3v5h5"/>
        </svg>
      </button>
    </div>

    <!-- Mini-map (bottom-left) -->
    <div v-if="props.nodes.length > 1" class="minimap">
      <svg :width="MM_W" :height="MM_H">
        <g v-for="n in props.nodes" :key="n.id">
          <rect v-if="positions[n.id]"
                :x="mmX(positions[n.id].x) - 5" :y="mmY(positions[n.id].y) - 3"
                :width="Math.max(8, nodeW(n) * MM_SCALE - 4)" :height="5"
                rx="1.5"
                :fill="NODE_COLORS[n.type] ?? '#888'"
                :opacity="isHighlighted(n) ? 1 : 0.65" />
        </g>
      </svg>
      <span class="mm-label">Map</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted, onUnmounted } from 'vue'
import type { NodeDto, EdgeDto } from '@/api/catalog'
import { palette } from '@/theme/palette'

const props = defineProps<{
  nodes: NodeDto[]
  edges: EdgeDto[]
  highlightIds?: string[]
  layout?: string
  selectedId?: string | null
}>()

const emit = defineEmits<{
  nodeClick: [id: string]
  nodeRightClick: [id: string, x: number, y: number]
  edgeRightClick: [id: string, x: number, y: number]
  deselect: []
}>()

// ── Constants ───────────────────────────────────────────────────────────────
const NODE_H = 44
const BADGE   = 26
const BADGE_X = 10
const ICON    = 16
const MIN_W   = 100
const MAX_W   = 260
const TEXT_X  = BADGE_X + BADGE + 8

const MM_W = 180
const MM_H = 108
const MM_SCALE = 0.05

const NODE_COLORS: Record<string, string> = {
  Service:  palette.nodeService,
  Server:   palette.nodeServer,
  Database: palette.nodeDatabase,
}

// ── Viewport + size ─────────────────────────────────────────────────────────
const vp        = reactive({ x: 0, y: 0, k: 1 })
const container = ref<HTMLElement | null>(null)
const size      = reactive({ w: 0, h: 0 })

// ── Reactive positions (updated by physics sim every RAF) ──────────────────
const positions = reactive<Record<string, { x: number; y: number }>>({})

// ── Interaction state ───────────────────────────────────────────────────────
const hoverId    = ref<string | null>(null)
const draggingId = ref<string | null>(null)
const clickedId  = ref<string | null>(null)

// Keep clickedId in sync with the selectedId prop (driven by sidebar / grid / inspector)
watch(() => props.selectedId, id => { clickedId.value = id ?? null })

// 1-hop neighbourhood of clickedId (clicked node + directly connected nodes)
const clickNeighbours = computed<Set<string> | null>(() => {
  if (!clickedId.value) return null
  const set = new Set([clickedId.value])
  props.edges.forEach(e => {
    if (e.fromId === clickedId.value) set.add(e.toId)
    if (e.toId   === clickedId.value) set.add(e.fromId)
  })
  return set
})

// ── Text measurement ────────────────────────────────────────────────────────
const _mctx = (() => {
  const c = document.createElement('canvas')
  return c.getContext('2d')!
})()
const _wCache: Record<string, number> = {}
const NODE_FONT = '600 13px Inter, ui-sans-serif, system-ui, "Segoe UI", sans-serif'

function nodeW(n: NodeDto): number {
  if (_wCache[n.name] != null) return _wCache[n.name]
  _mctx.font = NODE_FONT
  const w = _mctx.measureText(n.name).width
  _wCache[n.name] = Math.max(MIN_W, Math.min(MAX_W, Math.ceil(w + TEXT_X + 14)))
  return _wCache[n.name]
}

function nodeTransform(n: NodeDto): string {
  const p = positions[n.id]
  if (!p) return 'translate(0,0)'
  return `translate(${p.x - nodeW(n) / 2},${p.y - NODE_H / 2})`
}

// ── Physics simulation ──────────────────────────────────────────────────────
interface Particle { x: number; y: number; vx: number; vy: number; hx: number; hy: number; fx: number; fy: number }

const _sim: Record<string, Particle> = {}
let _simRunning = false
let _raf = 0
let _drag: { id: string; tx: number; ty: number } | null = null

const REPULSION = 7000
const ANCHOR_K  = 0.022
const EDGE_K    = 0.010
const EDGE_REST = 190
const DAMPING   = 0.78
const DT        = 1

function _startSim() {
  if (_simRunning) return
  _simRunning = true
  _simStep()
}

function _simStep() {
  const ids = Object.keys(_sim)
  for (const id of ids) { _sim[id].fx = 0; _sim[id].fy = 0 }

  // Anchor spring
  for (const id of ids) {
    const p = _sim[id]
    p.fx += (p.hx - p.x) * ANCHOR_K
    p.fy += (p.hy - p.y) * ANCHOR_K
  }

  // Edge springs
  for (const e of props.edges) {
    const a = _sim[e.fromId], b = _sim[e.toId]
    if (!a || !b) continue
    const dx = b.x - a.x, dy = b.y - a.y
    const d  = Math.hypot(dx, dy) || 0.001
    const f  = (d - EDGE_REST) * EDGE_K
    const ux = dx / d, uy = dy / d
    a.fx += ux * f; a.fy += uy * f
    b.fx -= ux * f; b.fy -= uy * f
  }

  // Pairwise repulsion
  for (let i = 0; i < ids.length; i++) {
    const a = _sim[ids[i]]
    for (let j = i + 1; j < ids.length; j++) {
      const b = _sim[ids[j]]
      const dx = b.x - a.x, dy = b.y - a.y
      const d2 = dx * dx + dy * dy + 80
      if (d2 > 360 * 360) continue
      const d  = Math.sqrt(d2)
      const f  = REPULSION / d2
      const ux = dx / d, uy = dy / d
      a.fx -= ux * f; a.fy -= uy * f
      b.fx += ux * f; b.fy += uy * f
    }
  }

  // Integrate
  let energy = 0
  for (const id of ids) {
    const p = _sim[id]
    p.vx = (p.vx + p.fx * DT) * DAMPING
    p.vy = (p.vy + p.fy * DT) * DAMPING
    p.x += p.vx * DT
    p.y += p.vy * DT
    energy += p.vx * p.vx + p.vy * p.vy
  }

  // Drag override
  if (_drag && _sim[_drag.id]) {
    const p = _sim[_drag.id]
    p.vx = _drag.tx - p.x
    p.vy = _drag.ty - p.y
    p.x  = _drag.tx
    p.y  = _drag.ty
    energy = Math.max(energy, 1)
  }

  // Write to reactive positions (Vue batches to next microtask)
  for (const id of ids) {
    if (positions[id]) {
      positions[id].x = _sim[id].x
      positions[id].y = _sim[id].y
    } else {
      positions[id] = { x: _sim[id].x, y: _sim[id].y }
    }
  }

  if (energy > 0.08 || _drag) {
    _raf = requestAnimationFrame(_simStep)
  } else {
    _simRunning = false
  }
}

function _computeHomes(): Record<string, { x: number; y: number }> {
  const nodes  = props.nodes
  const layout = props.layout ?? 'cose'

  if (layout === 'circle') {
    const cx = 500, cy = 320
    const r  = Math.max(180, nodes.length * 28)
    const out: Record<string, { x: number; y: number }> = {}
    nodes.forEach((n, i) => {
      const a = (i / Math.max(1, nodes.length)) * Math.PI * 2 - Math.PI / 2
      out[n.id] = { x: cx + Math.cos(a) * r, y: cy + Math.sin(a) * r }
    })
    return out
  }

  if (layout === 'breadthfirst') {
    const byType: Record<string, NodeDto[]> = { Server: [], Database: [], Service: [] }
    nodes.forEach(n => (byType[n.type] ??= []).push(n))
    const out: Record<string, { x: number; y: number }> = {}
    ;['Server', 'Database', 'Service'].forEach((t, col) => {
      ;(byType[t] || []).forEach((n, i) => {
        out[n.id] = { x: 160 + col * 340, y: 100 + i * 120 }
      })
    })
    return out
  }

  // Default: type-based 3-column (cose uses physics on top of this)
  const byType: Record<string, NodeDto[]> = { Service: [], Server: [], Database: [] }
  nodes.forEach(n => (byType[n.type] ??= []).push(n))
  const out: Record<string, { x: number; y: number }> = {}
  ;['Service', 'Server', 'Database'].forEach((t, col) => {
    ;(byType[t] || []).forEach((n, i) => {
      out[n.id] = { x: 180 + col * 300, y: 120 + i * 110 }
    })
  })
  return out
}

function _syncSim() {
  const homes = _computeHomes()
  const next: Record<string, Particle> = {}
  props.nodes.forEach(n => {
    const home = homes[n.id] ?? { x: 300, y: 300 }
    const prev = _sim[n.id]
    next[n.id] = prev
      ? { ...prev, hx: home.x, hy: home.y }
      : { x: home.x, y: home.y, vx: 0, vy: 0, hx: home.x, hy: home.y, fx: 0, fy: 0 }
    if (!positions[n.id]) positions[n.id] = { x: next[n.id].x, y: next[n.id].y }
  })
  // prune removed nodes
  for (const k in _sim) { if (!next[k]) delete _sim[k] }
  Object.assign(_sim, next)
  _startSim()
}

// ── Edge path ────────────────────────────────────────────────────────────────
function edgePath(e: EdgeDto): { d: string; mx: number; my: number } | null {
  const a = positions[e.fromId], b = positions[e.toId]
  if (!a || !b) return null
  const dx = b.x - a.x, dy = b.y - a.y
  const dist = Math.hypot(dx, dy)
  if (dist < 1) return null
  const nx = dx / dist, ny = dy / dist

  const fromN = props.nodes.find(n => n.id === e.fromId)
  const toN   = props.nodes.find(n => n.id === e.toId)
  if (!fromN || !toN) return null

  const trim = (node: NodeDto) => {
    const W = nodeW(node), H = NODE_H
    const tx = nx === 0 ? Infinity : (W / 2) / Math.abs(nx)
    const ty = ny === 0 ? Infinity : (H / 2) / Math.abs(ny)
    return Math.min(tx, ty) + 5
  }

  const sx = a.x + nx * trim(fromN), sy = a.y + ny * trim(fromN)
  const ex = b.x - nx * trim(toN),   ey = b.y - ny * trim(toN)
  return { d: `M ${sx} ${sy} L ${ex} ${ey}`, mx: (sx + ex) / 2, my: (sy + ey) / 2 }
}

// ── Dim / highlight ──────────────────────────────────────────────────────────
function isHighlighted(n: NodeDto)  { return !!(props.highlightIds?.includes(n.id)) }
function isNodeDimmed(n: NodeDto) {
  if (clickNeighbours.value) return !clickNeighbours.value.has(n.id)
  return !!(props.highlightIds?.length && !props.highlightIds.includes(n.id))
}
function isEdgeDimmed(e: EdgeDto) {
  if (clickNeighbours.value) {
    return !(clickNeighbours.value.has(e.fromId) && clickNeighbours.value.has(e.toId))
  }
  if (!props.highlightIds?.length) return false
  return !(props.highlightIds.includes(e.fromId) && props.highlightIds.includes(e.toId))
}
function showEdgeLabel(e: EdgeDto) {
  if (!hoverId.value) return false
  return e.fromId === hoverId.value || e.toId === hoverId.value
}

// ── Minimap ──────────────────────────────────────────────────────────────────
const _mmBounds = computed(() => {
  const ps = Object.values(positions)
  if (!ps.length) return { minX: 0, maxX: 1, minY: 0, maxY: 1 }
  const xs = ps.map(p => p.x), ys = ps.map(p => p.y)
  const pad = 60
  return {
    minX: Math.min(...xs) - pad, maxX: Math.max(...xs) + pad,
    minY: Math.min(...ys) - pad, maxY: Math.max(...ys) + pad,
  }
})
function mmX(wx: number) {
  const { minX, maxX } = _mmBounds.value
  return ((wx - minX) / (maxX - minX)) * MM_W
}
function mmY(wy: number) {
  const { minY, maxY } = _mmBounds.value
  return ((wy - minY) / (maxY - minY)) * MM_H
}

// ── Fit view ─────────────────────────────────────────────────────────────────
function fitView() {
  const ps = Object.values(positions)
  if (!ps.length || !size.w || !size.h) return
  const xs = ps.map(p => p.x), ys = ps.map(p => p.y)
  const pad = 80
  const minX = Math.min(...xs) - pad, maxX = Math.max(...xs) + pad
  const minY = Math.min(...ys) - pad, maxY = Math.max(...ys) + pad
  const k = Math.max(0.15, Math.min(1.5, Math.min(size.w / (maxX - minX), size.h / (maxY - minY))))
  const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2
  vp.x = size.w / 2 - cx * k
  vp.y = size.h / 2 - cy * k
  vp.k = k
}

// ── Focus node (called externally) ───────────────────────────────────────────
function focusNode(id: string) {
  const p = positions[id]
  if (!p || !size.w || !size.h) return
  vp.x = size.w / 2 - p.x * vp.k
  vp.y = size.h / 2 - p.y * vp.k
  clickedId.value = id
}

// ── Zoom controls ────────────────────────────────────────────────────────────
function _zoomAt(factor: number, cx = size.w / 2, cy = size.h / 2) {
  const k2 = Math.max(0.15, Math.min(2.5, vp.k * factor))
  const r = k2 / vp.k
  vp.x = cx - (cx - vp.x) * r
  vp.y = cy - (cy - vp.y) * r
  vp.k = k2
}
function zoomIn()    { _zoomAt(1.25) }
function zoomOut()   { _zoomAt(0.8) }
function resetZoom() { _zoomAt(1 / vp.k) }

// ── Wheel ────────────────────────────────────────────────────────────────────
function onWheel(e: WheelEvent) {
  const rect = container.value!.getBoundingClientRect()
  const factor = 1 + (-e.deltaY * 0.0015)
  _zoomAt(factor, e.clientX - rect.left, e.clientY - rect.top)
}

// ── Canvas pan ────────────────────────────────────────────────────────────────
function onCanvasDown(e: MouseEvent) {
  if ((e.target as Element).closest('[data-node-id]')) return
  clickedId.value = null
  emit('deselect')
  const sx = e.clientX, sy = e.clientY, vx0 = vp.x, vy0 = vp.y
  const move = (ev: MouseEvent) => { vp.x = vx0 + (ev.clientX - sx); vp.y = vy0 + (ev.clientY - sy) }
  const up   = () => { window.removeEventListener('mousemove', move); window.removeEventListener('mouseup', up) }
  window.addEventListener('mousemove', move)
  window.addEventListener('mouseup', up)
}

// ── Node drag ────────────────────────────────────────────────────────────────
function onNodeDown(e: MouseEvent, n: NodeDto) {
  e.preventDefault()
  const rect = container.value!.getBoundingClientRect()
  const toWorld = (cx: number, cy: number) => ({
    x: (cx - rect.left - vp.x) / vp.k,
    y: (cy - rect.top  - vp.y) / vp.k,
  })
  const w0  = toWorld(e.clientX, e.clientY)
  const pos = positions[n.id] ?? { x: 0, y: 0 }
  const off = { dx: pos.x - w0.x, dy: pos.y - w0.y }

  draggingId.value = n.id
  _drag = { id: n.id, tx: pos.x, ty: pos.y }
  _startSim()

  let moved = false
  const move = (ev: MouseEvent) => {
    moved = true
    const w = toWorld(ev.clientX, ev.clientY)
    _drag = { id: n.id, tx: w.x + off.dx, ty: w.y + off.dy }
  }
  const up = () => {
    const p = _sim[n.id]
    if (p) { p.hx = p.x; p.hy = p.y; p.vx = 0; p.vy = 0 }
    _drag = null
    draggingId.value = null
    window.removeEventListener('mousemove', move)
    window.removeEventListener('mouseup', up)
    if (!moved) {
      clickedId.value = clickedId.value === n.id ? null : n.id
      emit('nodeClick', n.id)
    }
    _startSim()
  }
  window.addEventListener('mousemove', move)
  window.addEventListener('mouseup', up)
}

function onNodeCtx(e: Event, n: NodeDto) {
  const me = e as MouseEvent
  emit('nodeRightClick', n.id, me.clientX, me.clientY)
}
function onEdgeCtx(e: Event, edge: EdgeDto) {
  const me = e as MouseEvent
  emit('edgeRightClick', edge.id, me.clientX, me.clientY)
}

// ── Lifecycle ────────────────────────────────────────────────────────────────
let _ro: ResizeObserver | null = null

onMounted(() => {
  if (!container.value) return
  size.w = container.value.clientWidth
  size.h = container.value.clientHeight

  _ro = new ResizeObserver(() => {
    size.w = container.value!.clientWidth
    size.h = container.value!.clientHeight
  })
  _ro.observe(container.value)

  _syncSim()
  setTimeout(() => fitView(), 320)
})

onUnmounted(() => {
  _ro?.disconnect()
  cancelAnimationFrame(_raf)
})

watch(() => [props.nodes, props.edges] as const, () => _syncSim(), { deep: false })
watch(() => props.layout, () => {
  const homes = _computeHomes()
  props.nodes.forEach(n => {
    const p = _sim[n.id]
    if (p) { p.hx = homes[n.id]?.x ?? p.hx; p.hy = homes[n.id]?.y ?? p.hy }
  })
  _startSim()
  setTimeout(() => fitView(), 400)
})

defineExpose({ focusNode })
</script>

<style scoped>
.graph-canvas {
  position: absolute; inset: 0;
  background: v-bind('palette.graphBg');
  overflow: hidden;
  user-select: none;
}
.bg-svg {
  position: absolute; inset: 0;
  pointer-events: none;
}
.graph-svg {
  position: absolute; inset: 0;
  width: 100%; height: 100%;
  overflow: visible;
}
.zoom-controls {
  position: absolute; top: 16px; right: 16px; z-index: 10;
  display: flex; flex-direction: column; gap: 1px;
  background: v-bind('palette.divider');
  border: 1px solid v-bind('palette.divider');
  border-radius: 8px; overflow: hidden;
  box-shadow: var(--shadow-1);
}
.zoom-btn {
  width: 32px; height: 32px; border: 0; padding: 0;
  background: v-bind('palette.panel'); color: v-bind('palette.text2');
  display: flex; align-items: center; justify-content: center;
  cursor: pointer; transition: background .1s, color .1s;
}
.zoom-btn:hover { background: v-bind('palette.panel2'); color: v-bind('palette.text'); }
.zoom-sep { height: 1px; background: v-bind('palette.divider'); }
.zoom-pct {
  position: absolute; right: 16px; bottom: 16px; z-index: 10;
  display: flex; align-items: center; gap: 4px;
  font-size: 11px; font-family: var(--font-mono);
  color: v-bind('palette.muted');
  background: v-bind('palette.panel');
  border: 1px solid v-bind('palette.divider');
  border-radius: 6px; padding: 3px 4px 3px 8px;
}
.zoom-reset-btn {
  width: 20px; height: 20px; border: 0; padding: 0; border-radius: 4px;
  background: transparent; color: v-bind('palette.muted');
  display: flex; align-items: center; justify-content: center;
  cursor: pointer; transition: background .1s, color .1s;
}
.zoom-reset-btn:hover { background: v-bind('palette.panel2'); color: v-bind('palette.text'); }
.minimap {
  position: absolute; left: 16px; bottom: 16px; z-index: 10;
  background: v-bind('palette.panel');
  border: 1px solid v-bind('palette.divider');
  border-radius: 8px; overflow: hidden;
  box-shadow: var(--shadow-1);
}
.mm-label {
  position: absolute; top: 5px; left: 7px;
  font-size: 9px; font-family: var(--font-mono);
  color: v-bind('palette.muted');
  text-transform: uppercase; letter-spacing: .08em;
  pointer-events: none;
}
</style>
