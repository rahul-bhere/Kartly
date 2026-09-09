import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // IMPORTANT for GitHub Pages: set this to '/<your-repo-name>/' before
  // deploying (e.g. '/kartly/'), since the site is served from a
  // sub-path, not the domain root. Keep it as './' for local dev/build
  // testing. See the README's "Deploying to GitHub Pages" section.
  base: './',
  server: {
    port: 5173,
  },
})
