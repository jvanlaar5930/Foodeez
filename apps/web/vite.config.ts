/// <reference types="vitest" />
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import { resolve } from 'path';

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      '@foodeez/shared': resolve(__dirname, '../../packages/shared/src/index.ts'),
      // Brand artwork lives outside this app so mobile can use the same files. Aliased to the
      // folder rather than an entry point: these are assets imported by path, not a module.
      '@foodeez/brand': resolve(__dirname, '../../packages/brand'),
    },
  },
  // index.html asks for '/favicon.svg'. Serving the shared brand folder rather than a local
  // public/ keeps the icon in the one place both clients read their artwork from - apps/web
  // has no public assets of its own, so nothing is displaced by pointing this outside the app.
  publicDir: resolve(__dirname, '../../packages/brand/public'),
  // Vitest reuses this config, so the '@' and '@foodeez/shared' aliases above apply to tests
  // too. 'node' rather than a DOM environment: these cover stores and pure utilities, not
  // component rendering.
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
});
