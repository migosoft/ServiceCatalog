<template>
  <div class="app-shell">
    <TopBar
      v-model:view="view"
      :node-count="store.nodes.length"
      :edge-count="store.edges.length"
      @refresh="store.loadGraph()"
    />

    <div class="workspace">
      <Sidebar
        :nodes="store.nodes"
        :filter-types="filterTypes"
        :search="search"
        :selected-id="selectedId"
        :error="store.error"
        @update:search="search = $event"
        @update:filter-types="filterTypes = $event"
        @select="onSidebarSelect"
        @add-node="openAddNode"
        @add-edge="openAddEdge"
      />

      <main class="main-area">
        <GraphCanvas
          ref="graphCanvas"
          v-show="view === 'graph'"
          :nodes="displayNodes"
          :edges="displayEdges"
          :highlight-ids="highlightIds"
          :layout="layout"
          :selected-id="selectedId"
          @node-click="onNodeClick"
          @node-right-click="onNodeRightClick"
          @edge-right-click="onEdgeRightClick"
          @deselect="selectedId = null"
        />
        <GridView
          v-if="view === 'grid'"
          :nodes="displayNodes"
          :edges="displayEdges"
          :selected-id="selectedId"
          @select="focusNode"
        />
        <div v-if="store.loading" class="loading-overlay">Loading…</div>
      </main>

      <transition name="slide">
        <Inspector
          v-if="selectedNode"
          :node="selectedNode"
          :nodes="store.nodes"
          :edges="store.edges"
          @close="selectedId = null"
          @select="focusNode"
          @edit="openEditNode"
          @delete="confirmDeleteNode"
          @add-relation="openAddEdge"
          @patch="onPatchNode"
        />
      </transition>
    </div>

    <!-- Modals -->
    <NodeForm
      v-if="showNodeForm"
      :node="editingNode"
      @close="closeNodeForm"
      @saved="closeNodeForm"
    />
    <EdgeForm
      v-if="showEdgeForm"
      :default-from-id="edgeFromId"
      @close="showEdgeForm = false"
      @saved="showEdgeForm = false"
    />

    <!-- Context menus -->
    <div
      v-if="nodeCtx.visible"
      class="ctx-menu"
      :style="{ top: nodeCtx.y + 'px', left: nodeCtx.x + 'px' }"
      @mouseleave="nodeCtx.visible = false"
    >
      <div class="ctx-item" @click="ctxEdit">Edit</div>
      <div class="ctx-item danger" @click="ctxDelete">Delete</div>
      <div class="ctx-item" @click="ctxAddEdge">+ Relation from here</div>
    </div>
    <div
      v-if="edgeCtx.visible"
      class="ctx-menu"
      :style="{ top: edgeCtx.y + 'px', left: edgeCtx.x + 'px' }"
      @mouseleave="edgeCtx.visible = false"
    >
      <div class="ctx-item danger" @click="ctxDeleteEdge">Delete relation</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, type ComponentPublicInstance } from 'vue'
import { useCatalogStore } from '@/stores/catalog'
import { useHealthStore } from '@/stores/health'
import type { NodeDto, EdgeDto, GraphDto } from '@/api/catalog'
import TopBar from '@/components/TopBar.vue'
import Sidebar from '@/components/Sidebar.vue'
import Inspector from '@/components/Inspector.vue'
import GridView from '@/components/GridView.vue'
import GraphCanvas from '@/components/GraphCanvas.vue'
import NodeForm from '@/components/NodeForm.vue'
import EdgeForm from '@/components/EdgeForm.vue'

const store = useCatalogStore()
const healthStore = useHealthStore()
const graphCanvas = ref<{ focusNode: (id: string) => void } | null>(null)

// View state
const view = ref<'graph' | 'grid'>('graph')
const layout = ref('cose')

// Filters & search
const search = ref('')
const filterTypes = ref(new Set<string>())

// Search / expand mode
const searchMode = ref(false)
const searchGraph = ref<GraphDto | null>(null)
const expandedGraph = ref<GraphDto | null>(null)

const displayNodes = computed<NodeDto[]>(() => {
  let base: NodeDto[] = store.nodes
  if (expandedGraph.value) return mergeGraphs(searchGraph.value, expandedGraph.value).nodes
  if (searchGraph.value) base = searchGraph.value.nodes
  return base.filter(n => {
    if (filterTypes.value.size && !filterTypes.value.has(n.type)) return false
    if (search.value) {
      const q = search.value.toLowerCase()
      if (!n.name.toLowerCase().includes(q) && !(n.description ?? '').toLowerCase().includes(q) && !(n.properties?.['owner'] ?? '').toLowerCase().includes(q)) return false
    }
    return true
  })
})

const displayEdges = computed<EdgeDto[]>(() => {
  if (expandedGraph.value) return mergeGraphs(searchGraph.value, expandedGraph.value).edges
  if (searchGraph.value) return searchGraph.value.edges
  const ids = new Set(displayNodes.value.map(n => n.id))
  return store.edges.filter(e => ids.has(e.fromId) && ids.has(e.toId))
})

const highlightIds = computed<string[]>(() =>
  searchGraph.value ? searchGraph.value.nodes.map(n => n.id) : []
)

function mergeGraphs(base: GraphDto | null, extra: GraphDto): GraphDto {
  const nm = new Map<string, NodeDto>()
  const em = new Map<string, EdgeDto>()
  ;[...(base?.nodes ?? []), ...extra.nodes].forEach(n => nm.set(n.id, n))
  ;[...(base?.edges ?? []), ...extra.edges].forEach(e => em.set(e.id, e))
  return { nodes: [...nm.values()], edges: [...em.values()] }
}

// Selection
const selectedId = ref<string | null>(null)
const selectedNode = computed(() =>
  selectedId.value ? store.nodes.find(n => n.id === selectedId.value) ?? null : null
)

async function focusNode(id: string) {
  selectedId.value = id
}

async function onNodeClick(id: string) {
  if (searchMode.value) {
    const extra = await store.getNeighbors(id)
    expandedGraph.value = expandedGraph.value ? mergeGraphs(expandedGraph.value, extra) : extra
  }
  await focusNode(id)
}

async function onSidebarSelect(id: string) {
  selectedId.value = id
  graphCanvas.value?.focusNode(id)
}

// Patch node fields inline from Inspector
async function onPatchNode(id: string, data: Partial<NodeDto>) {
  const node = store.nodes.find(n => n.id === id)
  if (!node) return
  const d = data as any
  const name             = d.name            ?? node.name
  const description      = d.description     ?? node.description
  const owner            = ('owner'    in d ? d.owner    : node.properties?.['owner'])    ?? ''
  const operatingSystem  = ('os'       in d ? d.os       : node.properties?.['os'])       ?? ''
  const address          = ('address'  in d ? d.address  : node.properties?.['address'])  ?? ''
  const dbType           = ('db_type'  in d ? d.db_type  : node.properties?.['db_type'])  ?? ''
  const databaseAddress  = ('database_address' in d ? d.database_address  : node.properties?.['database_address'])  ?? ''
  const codeRepository   = ('code_repo' in d ? d.code_repo : node.properties?.['code_repo']) ?? ''
  const documentationUrl = ('docs_url'  in d ? d.docs_url  : node.properties?.['docs_url'])  ?? ''
  await store.updateNode(id, {
    name, description,
    operatingSystem:  operatingSystem  || undefined,
    owner:            owner            || undefined,
    address:          address          || undefined,
    dbType:           dbType           || undefined,
    databaseAddress:  databaseAddress  || undefined,
    codeRepository:   codeRepository   || undefined,
    documentationUrl: documentationUrl || undefined,
  })
}

// Node form
const showNodeForm = ref(false)
const editingNode = ref<NodeDto | undefined>(undefined)
function openAddNode() { editingNode.value = undefined; showNodeForm.value = true }
function openEditNode() { editingNode.value = selectedNode.value ?? undefined; showNodeForm.value = true; selectedId.value = null }
function closeNodeForm() { showNodeForm.value = false; editingNode.value = undefined }

// Edge form
const showEdgeForm = ref(false)
const edgeFromId = ref<string | undefined>(undefined)
function openAddEdge() { edgeFromId.value = selectedId.value ?? undefined; showEdgeForm.value = true; selectedId.value = null }

// Delete
async function confirmDeleteNode() {
  if (!selectedNode.value) return
  if (!confirm(`Delete "${selectedNode.value.name}"?`)) return
  await store.deleteNode(selectedNode.value.id)
  selectedId.value = null
}

// Context menus
const nodeCtx = ref({ visible: false, x: 0, y: 0, id: '' })
const edgeCtx = ref({ visible: false, x: 0, y: 0, id: '' })

function onNodeRightClick(id: string, x: number, y: number) {
  nodeCtx.value = { visible: true, x, y, id }
  edgeCtx.value.visible = false
}
function onEdgeRightClick(id: string, x: number, y: number) {
  edgeCtx.value = { visible: true, x, y, id }
  nodeCtx.value.visible = false
}
function ctxEdit() {
  const n = store.nodes.find(n => n.id === nodeCtx.value.id)
  if (n) { editingNode.value = n; showNodeForm.value = true }
  nodeCtx.value.visible = false
}
async function ctxDelete() {
  if (confirm('Delete this node?')) await store.deleteNode(nodeCtx.value.id)
  nodeCtx.value.visible = false
  if (selectedId.value === nodeCtx.value.id) selectedId.value = null
}
function ctxAddEdge() {
  edgeFromId.value = nodeCtx.value.id; showEdgeForm.value = true
  nodeCtx.value.visible = false
}
async function ctxDeleteEdge() {
  if (confirm('Delete this relation?')) await store.deleteEdge(edgeCtx.value.id)
  edgeCtx.value.visible = false
}

function onGlobalClick() {
  nodeCtx.value.visible = false
  edgeCtx.value.visible = false
}

onMounted(() => {
  store.loadGraph()
  healthStore.connect()
  document.addEventListener('click', onGlobalClick)
})
onUnmounted(() => {
  document.removeEventListener('click', onGlobalClick)
  healthStore.disconnect()
})
</script>

<style scoped>
.app-shell { display: flex; flex-direction: column; height: 100vh; background: var(--c-page); color: var(--c-text); }
.workspace { flex: 1; display: flex; min-height: 0; }
.main-area { flex: 1; position: relative; overflow: hidden; background: var(--c-graph); }
.loading-overlay {
  position: absolute; inset: 0; display: flex; align-items: center; justify-content: center;
  color: var(--c-muted); font-size: 13px; background: rgba(250,246,236,.7); pointer-events: none;
}
.ctx-menu {
  position: fixed; z-index: 200; background: var(--c-panel);
  border: 1px solid var(--c-divider); border-radius: 8px; padding: 4px 0;
  min-width: 168px; box-shadow: var(--shadow-2);
}
.ctx-item { padding: 8px 14px; cursor: pointer; font-size: 13px; color: var(--c-text); }
.ctx-item:hover { background: var(--c-panel-2); }
.ctx-item.danger { color: var(--c-err); }
.slide-enter-active, .slide-leave-active { transition: width .18s ease, opacity .18s ease; }
.slide-enter-from, .slide-leave-to { width: 0 !important; opacity: 0; overflow: hidden; }
</style>
