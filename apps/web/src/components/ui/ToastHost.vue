<script setup lang="ts">
import AppAlert from '@/components/ui/AppAlert.vue';
import { useToastStore, type ToastVariant } from '@/stores/toast';

const toastStore = useToastStore();

/**
 * A confirmation is worth hearing after whatever the reader is doing; a failure interrupts.
 * `status` announces politely, `alert` cuts in - the difference between "meal logged" and
 * "that meal could not be saved".
 */
function roleFor(variant: ToastVariant): string {
  return variant === 'error' || variant === 'warning' ? 'alert' : 'status';
}
</script>

<template>
  <Teleport to="body">
    <!--
      Above the modal layer (z-50) on purpose: a save that fails inside a dialog has to be
      readable without closing it. `bottom-20` clears the mobile navigation bar, which the
      large layout does not have.
    -->
    <div
      class="pointer-events-none fixed inset-x-0 bottom-20 z-[60] flex flex-col items-stretch gap-2 px-4 sm:items-end sm:px-6 lg:bottom-6"
    >
      <TransitionGroup
        enter-active-class="transition-all duration-200 ease-out"
        enter-from-class="opacity-0 translate-y-2 scale-95"
        enter-to-class="opacity-100 translate-y-0 scale-100"
        leave-active-class="transition-all duration-150 ease-in"
        leave-from-class="opacity-100 scale-100"
        leave-to-class="opacity-0 scale-95"
        move-class="transition-transform duration-200"
      >
        <div
          v-for="toast in toastStore.toasts"
          :key="toast.id"
          class="pointer-events-auto w-full rounded-xl bg-white shadow-lg sm:max-w-sm dark:bg-gray-900"
        >
          <!-- The alert's own colours are semi-transparent, so it sits on a solid card here
               rather than letting the page show through what it is reporting on. -->
          <AppAlert
            :variant="toast.variant"
            :title="toast.title"
            :message="toast.message"
            :icon="toast.icon"
            :role="roleFor(toast.variant)"
            dismissible
            @dismiss="toastStore.dismiss(toast.id)"
          />
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
