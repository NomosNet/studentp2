<script setup>
import { computed, onMounted, ref } from 'vue'
import { getAdminCompanies, getAdminSummary } from '../../services/adminService'
import { downloadCsv } from '../../utils/csv'

const summary = ref(null)
const rows = ref([])
const error = ref('')

const cards = computed(() => [
  { label: 'Всего кликов', value: Number(summary.value?.total_clicks || 0).toLocaleString('ru-RU'), tone: 'purple' },
  { label: 'Партнерских компаний', value: Number(summary.value?.partners_count || 0).toLocaleString('ru-RU'), tone: 'green' },
  { label: 'Активных скидок', value: Number(summary.value?.active_ads_count || 0).toLocaleString('ru-RU'), tone: 'blue' },
])

function downloadReport() {
  downloadCsv('admin-statistics.csv', [
    ['Компания', 'Клики', 'Скидки'],
    ...rows.value.map((row) => [row.company, row.clicks, row.discounts]),
  ])
}

onMounted(async () => {
  try {
    const [summaryResponse, companies] = await Promise.all([getAdminSummary(), getAdminCompanies()])
    summary.value = summaryResponse
    rows.value = Array.isArray(companies) ? companies : []
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось загрузить статистику'
  }
})
</script>

<template>
  <div class="admin-page admin-container">
    <header class="admin-page-head admin-page-head--row">
      <div class="admin-page-title">
        <span class="admin-page-icon admin-page-icon--green" aria-hidden="true" />
        <div>
          <h1>Статистика</h1>
          <p>Клики и число скидок по компаниям</p>
        </div>
      </div>
      <button type="button" class="admin-btn admin-btn--primary" @click="downloadReport">Скачать отчёт</button>
    </header>

    <p v-if="error" class="admin-empty">{{ error }}</p>

    <section class="admin-summary-row">
      <article v-for="s in cards" :key="s.label" class="admin-summary-card" :class="`tone-${s.tone}`">
        <div>
          <strong>{{ s.value }}</strong>
          <span>{{ s.label }}</span>
        </div>
      </article>
    </section>

    <section class="admin-panel">
      <div class="admin-table-wrap">
        <table class="admin-table admin-table--wide">
          <thead>
            <tr>
              <th>Компания</th>
              <th>Клики</th>
              <th>Скидки</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!rows.length">
              <td colspan="3">Компаний пока нет</td>
            </tr>
            <tr v-for="row in rows" :key="row.id">
              <td>{{ row.company }}</td>
              <td>{{ Number(row.clicks || 0).toLocaleString('ru-RU') }}</td>
              <td>{{ row.discounts }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
