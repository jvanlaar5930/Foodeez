import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import { useThemeStore } from './stores/theme';
import './assets/main.css';

const app = createApp(App);
app.use(createPinia());
app.use(router);

// Resolve the theme before the first paint so there is no light-mode flash.
useThemeStore().init();

app.mount('#app');
