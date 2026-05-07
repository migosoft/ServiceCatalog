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
        <span class="logo-dot" />
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
      <div class="avatar">AS</div>
    </div>
  </header>
</template>

<script setup lang="ts">
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
}
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
.avatar {
  width: 28px; height: 28px; margin-left: 6px; border-radius: 999px;
  background: linear-gradient(135deg, var(--c-accent), var(--c-server));
  color: #fff; display: flex; align-items: center; justify-content: center;
  font-size: 11px; font-weight: 600; font-family: var(--font-mono);
}
</style>
