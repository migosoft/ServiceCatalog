<template>
  <header class="topbar">
    <div class="logo-area">
      <div class="logo">
        <svg width="16" height="16" viewBox="0 0 138 128" fill="none">
          <circle cx="24"  cy="24"  r="12" fill="currentColor"/>
          <circle cx="84"  cy="40"  r="12" fill="currentColor"/>
          <circle cx="44"  cy="94"  r="12" fill="currentColor"/>
          <circle cx="114" cy="104" r="12" fill="currentColor"/>
          <path d="M38.85 28.32 L69 39 M54.61 83.39 L75.99 54.04 M59 98 L99 104 M91.71 54.04 L108.39 90.71"
                stroke="currentColor" stroke-width="7" stroke-linecap="round"/>
        </svg>
        <span class="logo-dot" :class="logoDotClass" />
      </div>
      <div class="logo-text">
        <div class="logo-title">Service Catalog</div>
        <div class="logo-sub">production · local</div>
      </div>
    </div>

    <div class="divider-v" />

    <div class="view-switcher">
      <button
        v-for="v in VIEWS"
        :key="v.id"
        :class="['view-btn', { active: view === v.id }]"
        @click="$emit('update:view', v.id)"
      >
        <svg v-if="v.id === 'graph'" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="9"/><path d="m9 15 1.5-4.5L15 9l-1.5 4.5z"/>
        </svg>
        <svg v-else width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
          <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
        </svg>
        {{ v.label }}
      </button>
    </div>

    <div class="spacer" />

    <div class="stats">
      <div class="stat"><span class="stat-val">{{ nodeCount }}</span> nodes</div>
      <div class="stat"><span class="stat-val">{{ edgeCount }}</span> relations</div>
    </div>

    <div class="actions">
      <button class="icon-btn" title="Refresh" @click="$emit('refresh')">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">
          <path d="M21 12a9 9 0 1 1-3-6.7L21 8"/><path d="M21 3v5h-5"/>
        </svg>
      </button>
      <a
        href="https://github.com/migosoft/ServiceCatalog"
        target="_blank"
        rel="noopener noreferrer"
        class="github-link"
        title="View on GitHub"
      >
        <svg width="18" height="18" viewBox="0 0 98 96" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
          <path fill-rule="evenodd" clip-rule="evenodd" d="M48.854 0C21.839 0 0 22 0 49.217c0 21.756 13.993 40.172 33.405 46.69 2.427.49 3.316-1.059 3.316-2.362 0-1.141-.08-5.052-.08-9.127-13.59 2.934-16.42-5.867-16.42-5.867-2.184-5.704-5.42-7.17-5.42-7.17-4.448-3.015.324-3.015.324-3.015 4.934.326 7.523 5.052 7.523 5.052 4.367 7.496 11.404 5.378 14.235 4.074.404-3.178 1.699-5.378 3.074-6.6-10.839-1.141-22.243-5.378-22.243-24.283 0-5.378 1.94-9.778 5.014-13.2-.485-1.222-2.184-6.275.486-13.038 0 0 4.125-1.304 13.426 5.052a46.97 46.97 0 0 1 12.214-1.63c4.125 0 8.33.571 12.213 1.63 9.302-6.356 13.427-5.052 13.427-5.052 2.67 6.763.97 11.816.485 13.038 3.155 3.422 5.015 7.822 5.015 13.2 0 18.905-11.404 23.06-22.324 24.283 1.78 1.548 3.316 4.481 3.316 9.126 0 6.6-.08 11.897-.08 13.526 0 1.304.89 2.853 3.316 2.364 19.412-6.52 33.405-24.935 33.405-46.691C97.707 22 75.788 0 48.854 0z"/>
        </svg>
      </a>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useHealthStore } from '@/stores/health'

defineProps<{
  view: string
  nodeCount: number
  edgeCount: number
}>()

defineEmits<{
  'update:view': [v: string]
  refresh: []
}>()

const VIEWS = [
  { id: 'graph', label: 'Graph' },
  { id: 'grid',  label: 'Grid' },
]

const health = useHealthStore()
const logoDotClass = computed(() => {
  if (!health.hasMonitored) return ''
  if (health.anyDown) return 'dot-down'
  if (health.anyCompromised) return 'dot-warn'
  return 'dot-up'
})
</script>

<style scoped>
.topbar {
  height: 52px; flex-shrink: 0;
  background: var(--c-panel); border-bottom: 1px solid var(--c-divider);
  display: flex; align-items: center; padding: 0 16px; gap: 16px;
}
.logo-area { display: flex; align-items: center; gap: 10px; }
.logo {
  width: 28px; height: 28px; border-radius: 7px; flex-shrink: 0;
  background: var(--c-text); color: var(--c-panel);
  display: flex; align-items: center; justify-content: center; position: relative;
}
.logo-dot {
  position: absolute; right: -2px; top: -2px; width: 8px; height: 8px;
  border-radius: 999px; background: var(--c-accent); border: 1.5px solid var(--c-panel);
  transition: background .3s;
}
.logo-dot.dot-up   { background: var(--c-ok); }
.logo-dot.dot-warn { background: var(--c-warn); animation: ldot-blink 1.2s ease-in-out infinite; }
.logo-dot.dot-down { background: var(--c-err);  animation: ldot-blink 1.2s ease-in-out infinite; }
@keyframes ldot-blink { 0%, 100% { opacity: 1; } 50% { opacity: 0.25; } }
.logo-text { display: flex; flex-direction: column; line-height: 1.15; }
.logo-title { font-size: 13.5px; font-weight: 600; color: var(--c-text); letter-spacing: -0.005em; }
.logo-sub { font-size: 10.5px; color: var(--c-muted); font-family: var(--font-mono); letter-spacing: 0.02em; }
.divider-v { width: 1px; height: 22px; background: var(--c-divider); flex-shrink: 0; }
.view-switcher { display: flex; background: var(--c-panel-2); border-radius: 8px; padding: 2px; gap: 1px; }
.view-btn {
  display: flex; align-items: center; gap: 5px; padding: 5px 10px;
  border: 0; border-radius: 6px; background: transparent; color: var(--c-muted);
  font-size: 12px; font-weight: 500; cursor: pointer; transition: background .1s, color .1s;
}
.view-btn.active { background: var(--c-panel); color: var(--c-text); box-shadow: var(--shadow-1); }
.spacer { flex: 1; }
.stats { display: flex; align-items: center; gap: 16px; font-size: 11.5px; color: var(--c-muted); font-family: var(--font-mono); }
.stat { display: flex; align-items: baseline; gap: 4px; }
.stat-val { font-size: 13px; font-weight: 600; color: var(--c-text); font-variant-numeric: tabular-nums; }
.actions { display: flex; align-items: center; gap: 4px; }
.icon-btn {
  width: 30px; height: 30px; border: 0; border-radius: 7px;
  background: transparent; color: var(--c-text-2);
  display: flex; align-items: center; justify-content: center; cursor: pointer;
}
.icon-btn:hover { background: var(--c-panel-2); }
.github-link {
  width: 30px; height: 30px; margin-left: 2px; border-radius: 7px;
  color: var(--c-text-2); display: flex; align-items: center; justify-content: center;
  text-decoration: none; transition: color .12s, background .12s;
}
.github-link:hover { background: var(--c-panel-2); color: var(--c-text); }
</style>
