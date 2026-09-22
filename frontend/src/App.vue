<script setup>
import AuthModals from './components/AuthModals.vue'
import AdminPartnerModals from './components/AdminPartnerModals.vue'
import CookieBanner from './components/CookieBanner.vue'
</script>

<template>
  <RouterView />
  <AuthModals />
  <AdminPartnerModals />
  <CookieBanner />
</template>
