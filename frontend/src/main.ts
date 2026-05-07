import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { cssVars } from './theme/palette'

// Stamp all palette values as CSS custom properties on :root
Object.entries(cssVars).forEach(([k, v]) => document.documentElement.style.setProperty(k, v))

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')
