<template>
  <div
    v-if="visible"
    class="context-menu"
    :style="{ top: y + 'px', left: x + 'px' }"
    @mouseleave="visible = false"
  >
    <slot />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const visible = ref(false)
const x = ref(0)
const y = ref(0)

function show(px: number, py: number) {
  x.value = px
  y.value = py
  visible.value = true
}
function hide() { visible.value = false }

defineExpose({ show, hide, visible })
</script>

<style scoped>
.context-menu {
  position: fixed; z-index: 200;
  background: #16213e; border: 1px solid #4f8ef7;
  border-radius: 6px; padding: 4px 0; min-width: 140px;
  box-shadow: 0 4px 16px rgba(0,0,0,.5);
}
</style>
