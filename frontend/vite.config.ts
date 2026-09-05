import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Bind to 0.0.0.0 so the Dev Containers port forwarder can reach it.
    host: true,
    port: 5173,
    strictPort: true,
    // Same-origin calls to /api are proxied to the ASP.NET Core API, which keeps
    // the browser free of CORS preflights during development.
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
      '/health': { target: 'http://localhost:5080', changeOrigin: true },
      '/openapi': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
})
