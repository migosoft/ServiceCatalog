<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-head">
        <span class="modal-title">New relation</span>
        <button class="modal-close" @click="$emit('close')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M6 6l12 12M18 6 6 18"/></svg>
        </button>
      </div>

      <div class="modal-body">
        <form @submit.prevent="submit">
          <!-- From node -->
          <div class="field">
            <label class="field-label">From</label>
            <select v-model="form.fromId" class="field-input" required>
              <option value="" disabled>Select source node…</option>
              <optgroup v-for="type in TYPE_ORDER" :key="type" :label="type">
                <option
                  v-for="n in grouped[type]"
                  :key="n.id"
                  :value="n.id"
                >{{ n.name }}</option>
              </optgroup>
            </select>
          </div>

          <!-- Relation type -->
          <div class="field">
            <label class="field-label">Relation type</label>
            <div class="rel-cards">
              <button
                v-for="r in RELATION_TYPES"
                :key="r.value"
                type="button"
                :class="['rel-card', { active: form.relationType === r.value, disabled: r.value === 'RUNS_ON' && !runsOnAllowed }]"
                :disabled="r.value === 'RUNS_ON' && !runsOnAllowed"
                @click="form.relationType = r.value"
              >
                <span class="rel-badge" :style="{ background: `var(--c-${r.value.toLowerCase().replace('_', '-')})` }" />
                <div class="rel-card-content">
                  <span class="rel-name">{{ r.value }}</span>
                  <span class="rel-hint">{{ r.hint }}</span>
                </div>
              </button>
            </div>
          </div>

          <!-- To node -->
          <div class="field">
            <label class="field-label">To</label>
            <select v-model="form.toId" class="field-input" required>
              <option value="" disabled>Select target node…</option>
              <optgroup v-for="type in TYPE_ORDER" :key="type" :label="type">
                <option
                  v-for="n in grouped[type]"
                  :key="n.id"
                  :value="n.id"
                >{{ n.name }}</option>
              </optgroup>
            </select>
          </div>

          <p v-if="error" class="error-msg">{{ error }}</p>

          <div class="field-actions">
            <button type="button" class="btn-secondary" @click="$emit('close')">Cancel</button>
            <button type="submit" class="btn-primary" :disabled="submitting">Create relation</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, watch } from 'vue'
import { useCatalogStore } from '@/stores/catalog'
import type { EdgeDto } from '@/api/catalog'

const props = defineProps<{ defaultFromId?: string; defaultToId?: string }>()
const emit = defineEmits<{ close: []; saved: [edge: EdgeDto] }>()

const store = useCatalogStore()
const submitting = ref(false)
const error = ref<string | null>(null)

const TYPE_ORDER = ['Service', 'Server', 'Database'] as const

const RELATION_TYPES = [
  { value: 'RUNS_ON',  hint: 'service or database → server only' },
  { value: 'REQUIRES', hint: 'any node depends on another' },
] as const

const form = reactive({
  fromId: props.defaultFromId ?? '',
  toId: props.defaultToId ?? '',
  relationType: 'RUNS_ON',
})

// RUNS_ON is only valid: from ∈ {Service, Database} → to = Server
const runsOnAllowed = computed(() => {
  const from = store.nodes.find(n => n.id === form.fromId)
  const to   = store.nodes.find(n => n.id === form.toId)
  if (from && from.type === 'Server') return false
  if (to   && to.type   !== 'Server') return false
  return true
})

watch(runsOnAllowed, allowed => {
  if (!allowed && form.relationType === 'RUNS_ON') form.relationType = 'REQUIRES'
})

const grouped = computed(() => {
  const g: Record<string, typeof store.nodes> = { Service: [], Server: [], Database: [] }
  store.nodes.forEach(n => { (g[n.type] ??= []).push(n) })
  Object.values(g).forEach(arr => arr.sort((a, b) => a.name.localeCompare(b.name)))
  return g
})

async function submit() {
  if (form.fromId === form.toId) { error.value = 'Source and target must differ.'; return }
  if (form.relationType === 'RUNS_ON' && !runsOnAllowed.value) {
    error.value = 'RUNS_ON is only valid from a Service or Database to a Server.'
    return
  }
  submitting.value = true
  error.value = null
  try {
    const edge = await store.createEdge(form)
    emit('saved', edge)
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
  width: 460px; background: var(--c-panel);
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
  transition: border-color .12s; font-family: inherit;
}
.field-input:focus { border-color: var(--c-accent); }
.rel-cards { display: flex; flex-direction: column; gap: 6px; }
.rel-card {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 12px; border-radius: 8px; cursor: pointer; text-align: left;
  border: 1px solid var(--c-divider); background: var(--c-panel); color: var(--c-text);
  transition: background .1s, border-color .1s; width: 100%;
}
.rel-card:hover { background: var(--c-panel-2); }
.rel-card.active { background: var(--c-panel-2); border-color: var(--c-text); }
.rel-card.disabled { opacity: .38; cursor: not-allowed; }
.rel-badge { width: 10px; height: 10px; border-radius: 3px; flex-shrink: 0; }
.rel-card-content { display: flex; flex-direction: column; gap: 2px; }
.rel-name { font-size: 12px; font-weight: 600; font-family: var(--font-mono); }
.rel-hint { font-size: 11px; color: var(--c-muted); }
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
