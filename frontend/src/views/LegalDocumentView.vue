<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import SitePublicHeader from '../components/SitePublicHeader.vue'
import { legalDocuments, legalMeta } from '../data/legalDocuments'

const route = useRoute()

const page = computed(() => legalDocuments[route.name] ?? null)
</script>

<template>
  <div class="legal-page">
    <SitePublicHeader />

    <main v-if="page" class="legal-main cat-container">
      <p class="how-eyebrow">{{ page.eyebrow }}</p>
      <h1 class="legal-title">{{ page.title }}</h1>
      <p class="legal-updated">{{ legalMeta.operator }} · обновлено {{ legalMeta.updated }}</p>
      <p class="legal-lead">{{ page.lead }}</p>

      <section v-for="section in page.sections" :key="section.title" class="legal-section">
        <h2>{{ section.title }}</h2>
        <p v-for="(paragraph, index) in section.paragraphs" :key="index">{{ paragraph }}</p>
      </section>
    </main>
  </div>
</template>
