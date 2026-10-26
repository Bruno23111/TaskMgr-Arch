import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // Troque VITE_API_URL para alternar entre a API MVC (5129) e a Clean Architecture (5278).
  const { VITE_API_URL = 'http://localhost:5278' } = loadEnv(mode, process.cwd())

  return {
    plugins: [react()],
    server: {
      // As APIs não habilitam CORS; o proxy deixa o navegador falar só com a origem do Vite.
      proxy: {
        '/api': { target: VITE_API_URL, changeOrigin: true, rewrite: (path) => path.replace(/^\/api/, '') },
      },
    },
  }
})
