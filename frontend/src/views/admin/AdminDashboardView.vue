<script setup>
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getAdminSummary } from '../../services/adminService'

const summary = ref(null)
const error = ref('')

const quickNav = [
  { title: 'Заявки компаний', desc: 'Новые заявки', to: '/admin/applications', tone: 'purple' },
  { title: 'Менеджеры', desc: 'Команда', to: '/admin/managers', tone: 'blue' },
  { title: 'Статистика', desc: 'Аналитика', to: '/admin/statistics', tone: 'green' },
  { title: 'Каталог', desc: 'Скидки на сайте', to: '/catalog', tone: 'purple' },
]

const stats = computed(() => [
  { label: 'Всего пользователей', value: summary.value?.users_count ?? 0, tone: 'blue' },
  { label: 'Активных скидок', value: summary.value?.active_ads_count ?? 0, tone: 'purple' },
  { label: 'Заявок компаний', value: summary.value?.pending_requests_count ?? 0, tone: 'purple' },
  { label: 'Партнерских компаний', value: summary.value?.partners_count ?? 0, tone: 'green' },
])

function formatCount(value) {
  return Number(value || 0).toLocaleString('ru-RU')
}

function formatDate(value) {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleDateString('ru-RU')
}

function relativeTime(value) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const minutes = Math.round((Date.now() - date.getTime()) / 60000)
  if (minutes < 1) return 'только что'
  if (minutes < 60) return `${minutes} мин назад`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `${hours} ч назад`
  return `${Math.round(hours / 24)} дн назад`
}

onMounted(async () => {
  try {
    summary.value = await getAdminSummary()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось загрузить панель'
  }
})
</script>

<template>
  <div class="admin-page admin-container">
    <header class="admin-page-head">
      <div class="admin-page-title">
        <span class="admin-page-icon admin-page-icon--orange" aria-hidden="true" />
        <div>
          <h1>Панель администратора</h1>
          <p>Управление платформой StudentPass</p>
        </div>
      </div>
    </header>

    <p v-if="error" class="admin-empty">{{ error }}</p>

    <section class="admin-stat-grid">
      <article v-for="s in stats" :key="s.label" class="admin-stat-card" :class="`tone-${s.tone}`">
        <div>
          <strong>{{ formatCount(s.value) }}</strong>
          <span>{{ s.label }}</span>
        </div>
      </article>
    </section>

    <div class="admin-two-col">
      <section class="admin-panel">
        <h2>Последняя активность</h2>
        <p v-if="!summary?.activity?.length" class="admin-empty">Пока нет событий</p>
        <ul v-else class="admin-activity">
          <li v-for="(a, i) in summary.activity" :key="i" class="admin-activity__item">
            <span class="admin-activity__dot" :class="`dot-${a.tone}`" />
            <div>
              <p>{{ a.text }}</p>
              <time>{{ relativeTime(a.created_at) }}</time>
            </div>
          </li>
        </ul>
      </section>

      <section class="admin-panel">
        <div class="admin-panel-head">
          <h2>Заявки на рассмотрении</h2>
          <RouterLink :to="{ name: 'admin-applications' }" class="admin-link-all">Все →</RouterLink>
        </div>
        <p v-if="!summary?.pending_requests?.length" class="admin-empty">Нет заявок на рассмотрении</p>
        <div v-else class="admin-pending-list">
          <article v-for="p in summary.pending_requests" :key="p.id" class="admin-pending-card">
            <div class="admin-pending-head">
              <strong>{{ p.company_name }}</strong>
              <span class="admin-badge admin-badge--pending">На рассмотрении</span>
            </div>
            <p class="admin-pending-contact">{{ p.contact_person }}</p>
            <p class="admin-pending-offer">{{ p.description || 'Описание не указано' }}</p>
            <time>{{ formatDate(p.created_at) }}</time>
          </article>
        </div>
      </section>
    </div>

    <section class="admin-panel">
      <div class="admin-panel-head">
        <h2>Топ компаний по кликам</h2>
        <RouterLink :to="{ name: 'admin-statistics' }" class="admin-link-all">Подробная статистика →</RouterLink>
      </div>
      <div class="admin-table-wrap">
        <table class="admin-table">
          <thead>
            <tr>
              <th>Компания</th>
              <th>Клики</th>
              <th>Скидки</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!summary?.top_companies?.length">
              <td colspan="3">Кликов пока нет</td>
            </tr>
            <tr v-for="row in summary?.top_companies || []" :key="row.company">
              <td>{{ row.company }}</td>
              <td>{{ formatCount(row.clicks) }}</td>
              <td>{{ formatCount(row.discounts) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="admin-quick-grid">
      <RouterLink v-for="q in quickNav" :key="q.title" :to="q.to" class="admin-quick-card" :class="`tone-${q.tone}`">
        <strong>{{ q.title }}</strong>
        <span>{{ q.desc }}</span>
      </RouterLink>
    </section>
  </div>
</template>
