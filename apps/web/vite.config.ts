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
    },
  },
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
