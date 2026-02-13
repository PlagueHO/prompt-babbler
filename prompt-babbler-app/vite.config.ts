import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// Aspire injects service URLs as process env vars via WithReference().
// Forward the API service URL to the browser via Vite's define.
const apiBaseUrl = process.env.services__api__http__0 ?? ''

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  define: {
    __API_BASE_URL__: JSON.stringify(apiBaseUrl),
  },
})
