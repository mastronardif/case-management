import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  base: '/',   // ALWAYS root

  server: {
    host: true,
    proxy: {
      '/api': {
        target: 'https://localhost:44344',
        // target: 'http://localhost', // port 80
        secure: false
      }
    }
  },

  build: {
    outDir: 'dist',
    emptyOutDir: true
  }
});
