// ─────────────────────────────────────────────
//  Edit this file to switch the entire palette.
// ─────────────────────────────────────────────

export const palette = {
  // Backgrounds
  page:          '#FAF6EC',
  panel:         '#FFFDF7',
  panel2:        '#F5EDD8',
  graphBg:       '#F1E8D0',

  // Text
  text:          '#1A1815',
  text2:         '#4A4435',
  muted:         '#8A7A5A',

  // Borders & inputs
  divider:       '#E4D7B5',
  dividerStrong: '#C8B98F',
  inputBg:       '#FFFFFF',

  // Brand accent
  accent:        '#E25A1C',
  accentSoft:    '#FBE4D2',
  accentLine:    '#F5C7A3',

  // Node fill colors
  nodeService:   '#E25A1C',   // orange  – circle
  nodeServer:    '#1F1B14',   // near-black – rectangle
  nodeDatabase:  '#6F8F4C',   // moss green – barrel

  // Edge colors
  edgeRunsOn:    '#1F1B14',   // solid dark
  edgeRequires:  '#E25A1C',   // dashed accent

  // Status
  ok:            '#00bf7f',
  warn:          '#FFF700',
  err:           '#FF2C2C',
} as const

export type Palette = typeof palette

export const cssVars: Record<string, string> = {
  '--c-page':           palette.page,
  '--c-panel':          palette.panel,
  '--c-panel-2':        palette.panel2,
  '--c-graph':          palette.graphBg,
  '--c-text':           palette.text,
  '--c-text-2':         palette.text2,
  '--c-muted':          palette.muted,
  '--c-divider':        palette.divider,
  '--c-divider-strong': palette.dividerStrong,
  '--c-input':          palette.inputBg,
  '--c-accent':         palette.accent,
  '--c-accent-soft':    palette.accentSoft,
  '--c-accent-line':    palette.accentLine,
  '--c-service':        palette.nodeService,
  '--c-server':         palette.nodeServer,
  '--c-database':       palette.nodeDatabase,
  '--c-runs-on':        palette.edgeRunsOn,
  '--c-requires':       palette.edgeRequires,
  '--c-ok':             palette.ok,
  '--c-warn':           palette.warn,
  '--c-err':            palette.err,
  '--shadow-1':  '0 1px 0 rgba(31,27,20,.04), 0 1px 2px rgba(31,27,20,.06)',
  '--shadow-2':  '0 1px 0 rgba(31,27,20,.04), 0 6px 18px -6px rgba(31,27,20,.18)',
  '--shadow-pop':'0 24px 60px -20px rgba(31,27,20,.35), 0 2px 6px rgba(31,27,20,.08)',
  '--font-sans': "'Inter', ui-sans-serif, system-ui, -apple-system, 'Segoe UI', sans-serif",
  '--font-mono': "ui-monospace, 'Cascadia Code', 'SF Mono', Menlo, monospace",
}
