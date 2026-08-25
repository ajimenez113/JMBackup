import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// El build de producción escribe directo en wwwroot de JMBackup.Api (ver README):
// JMBackup.Api sirve la interfaz como archivos estáticos, no hay un servidor Node en
// producción. En desarrollo, el proxy manda /api y /hubs al Kestrel local — el
// certificado autofirmado (ADR de la fase 2) no es de una CA conocida, por eso
// `secure: false` en el proxy.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: '../src/JMBackup.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': { target: 'https://127.0.0.1:8483', changeOrigin: true, secure: false },
      '/hubs': { target: 'https://127.0.0.1:8483', changeOrigin: true, secure: false, ws: true },
    },
  },
})
