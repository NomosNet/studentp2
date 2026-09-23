<script setup>
import { computed, onMounted } from 'vue'
import { useManagerDiscounts } from '../../composables/useManagerDiscounts'
import { downloadCsv } from '../../utils/csv'

const { items, totalClicks, load, isManager, selectedPartnerId } = useManagerDiscounts()

const summary = computed(() => [
  { label: 'Скидок', value: String(items.value.length), tone: 'purple' },
  { label: 'Кликов', value: totalClicks.value.toLocaleString('ru-RU'), tone: 'blue' },
])

onMounted(() => {
  void load()
})

function downloadReport() {
  downloadCsv('manager-statistics.csv', [
    ['Предложение', 'Клики'],
    ...items.value.map((row) => [row.title, row.clicks]),
  ])
}
</script>

<template>
  <div class="admin-page admin-container">
    <header class="admin-page-head admin-page-head--row">
      <div class="admin-page-title">
        <span class="admin-page-icon admin-page-icon--green" aria-hidden="true" />
        <div>
          <h1>Статистика</h1>
          <p>Клики по скидкам выбранной компании</p>
        </div>
      </div>
      <button type="button" class="admin-btn admin-btn--primary" @click="downloadReport">Скачать отчёт</button>
    </header>

    <p v-if="isManager && !selectedPartnerId" class="admin-empty">Нет закреплённых компаний.</p>

    <template v-else>
      <section class="mgr-stat-summary">
        <article v-for="s in summary" :key="s.label" class="mgr-stat-summary__card" :class="`tone-${s.tone}`">
          <div>
            <strong>{{ s.value }}</strong>
            <span>{{ s.label }}</span>
          </div>
        </article>
      </section>

      <section class="admin-panel">
        <h2>Скидки</h2>
        <div class="admin-table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Предложение</th>
                <th>Клики</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!items.length">
                <td colspan="2">Скидок пока нет</td>
              </tr>
              <tr v-for="row in items" :key="row.id">
                <td>{{ row.title }}</td>
                <td>{{ row.clicks.toLocaleString('ru-RU') }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>
  </div>
</template>
