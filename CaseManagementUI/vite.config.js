import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  base: '/',   // ALWAYS root

  server: {
    host: true,
    // Vite validates the request Host header separately from the network interface it
    // binds to (server.host above). Without this, LAN access via a raw IP (e.g. phone on
    // the home network) gets rejected even though the TCP connection succeeds. Safe here
    // since this dev server is never exposed beyond the home network.
    allowedHosts: true,
    proxy: {
      '/api': {
        target: 'https://localhost:44344',
        // target: 'http://localhost', // port 80
        secure: false,
        changeOrigin: true
      },
      // Trailing slash matters: Vite's proxy match is a plain string-prefix check, and a
      // bare '/s' also matches '/src/*' (Vite's own dev source paths), breaking the whole
      // app. '/s/' only matches real short-link paths like /s/17.
      '/s/': {
        target: 'https://localhost:44344',
        secure: false,
        changeOrigin: true
      }
    }
  },

  build: {
    outDir: 'dist',
    emptyOutDir: true
  }
});
