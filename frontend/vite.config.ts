import { defineConfig } from 'vite'
import { reactRouter } from '@react-router/dev/vite'
import { siteRules } from './vite/site-rules-plugin.js'

// https://vite.dev/config/
export default defineConfig({
  // reactRouter() handles React itself, so @vitejs/plugin-react is not listed
  // here as well; running both would apply the React transform twice.
  // siteRules first, and enforce: 'pre' inside it, because React Router's dev
  // middleware answers an unrecognised address with the single-page document
  // and a 200. Anything installed after it can only change a response that has
  // already said the old address is fine.
  plugins: [siteRules(), reactRouter()],
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
