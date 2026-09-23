<script setup>
import { computed, onMounted, ref } from 'vue'
import { approvePartnerRequest, getPartnerRequests, rejectPartnerRequest } from '../../services/adminService'

const filter = ref('all')
const applications = ref([])
const error = ref('')
const rejectingEmail = ref('')
const rejectComment = ref('')

const filterOptions = [
  { key: 'all', label: 'Все' },
  { key: 'pending', label: 'На рассмотрении' },
  { key: 'approved', label: 'Одобренные' },
  { key: 'rejected', label: 'Отклоненные' },
]

const visible = computed(() => {
  if (filter.value === 'all') return applications.value
  return applications.value.filter((item) => item.status === filter.value)
})

const statusLabel = (status) => {
  if (status === 'pending') return 'На рассмотрении'
  if (status === 'approved') return 'Одобрена'
  return 'Отклонена'
}

const statusClass = (status) => {
  if (status === 'pending') return 'admin-badge--warn'
  if (status === 'approved') return 'admin-badge--ok'
  return 'admin-badge--bad'
}

function formatDate(value) {
  if (!value) return '-'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleDateString('ru-RU')
}

async function loadApplications() {
  error.value = ''
  const response = await getPartnerRequests({ limit: 100 })
  applications.value = (response?.items || []).map((item) => ({
    id: String(item.id),
    userEmail: item.user_email,
    company: item.company_name,
    status: item.status,
    contact: item.contact_person,
    email: item.user_email || '—',
    phone: item.phone,
    offer: item.description || 'Описание не указано',
    submitted: formatDate(item.created_at),
    resolved: item.status === 'pending' ? '' : formatDate(item.updated_at),
    reason: item.admin_comment || '',
  }))
}

async function approveRequest(application) {
  error.value = ''
  try {
    await approvePartnerRequest(application.userEmail)
    await loadApplications()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось одобрить заявку'
  }
}

function startReject(application) {
  rejectingEmail.value = application.userEmail
  rejectComment.value = ''
  error.value = ''
}

async function submitReject(application) {
  const comment = rejectComment.value.trim()
  if (!comment) {
    error.value = 'Укажите причину отклонения'
    return
  }
  error.value = ''
  try {
    await rejectPartnerRequest(application.userEmail, comment)
    rejectingEmail.value = ''
    await loadApplications()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось отклонить заявку'
  }
}

onMounted(() => {
  loadApplications().catch((err) => {
    error.value = err instanceof Error ? err.message : 'Не удалось загрузить заявки'
  })
})
</script>

<template>
  <div class="admin-page admin-container">
    <header class="admin-page-head">
      <div class="admin-page-title">
        <span class="admin-page-icon admin-page-icon--doc" aria-hidden="true" />
        <div>
          <h1>Заявки компаний</h1>
          <p>Управление заявками на партнерство</p>
        </div>
      </div>
    </header>

    <p v-if="error" class="admin-empty">{{ error }}</p>

    <div class="admin-filter-bar">
      <span class="admin-filter-label">Фильтр:</span>
      <div class="admin-filter-chips">
        <button
          v-for="opt in filterOptions"
          :key="opt.key"
          type="button"
          class="admin-chip"
          :class="{ 'is-active': filter === opt.key }"
          @click="filter = opt.key"
        >
          {{ opt.label }}
        </button>
      </div>
    </div>

    <p v-if="!visible.length" class="admin-empty">Заявок нет</p>

    <div class="admin-app-list">
      <article v-for="app in visible" :key="app.id" class="admin-app-card">
        <div class="admin-app-card__head">
          <div>
            <h2>{{ app.company }}</h2>
            <span class="admin-badge" :class="statusClass(app.status)">{{ statusLabel(app.status) }}</span>
          </div>
          <div v-if="app.status === 'pending'" class="admin-app-actions">
            <button type="button" class="admin-btn admin-btn--ok" @click="approveRequest(app)">Одобрить</button>
            <button type="button" class="admin-btn admin-btn--bad" @click="startReject(app)">Отклонить</button>
          </div>
        </div>

        <form
          v-if="rejectingEmail === app.userEmail"
          class="admin-reject-form"
          @submit.prevent="submitReject(app)"
        >
          <label class="admin-k" :for="`reject-${app.id}`">Причина отклонения</label>
          <textarea :id="`reject-${app.id}`" v-model="rejectComment" rows="3" class="admin-reject-input" />
          <div class="admin-app-actions">
            <button type="submit" class="admin-btn admin-btn--bad">Подтвердить отклонение</button>
            <button type="button" class="admin-btn admin-btn--outline" @click="rejectingEmail = ''">Отмена</button>
          </div>
        </form>

        <div class="admin-app-grid">
          <div>
            <span class="admin-k">Контактное лицо</span>
            <p>{{ app.contact }}</p>
          </div>
          <div>
            <span class="admin-k">Email</span>
            <p>{{ app.email }}</p>
          </div>
          <div>
            <span class="admin-k">Телефон</span>
            <p>{{ app.phone }}</p>
          </div>
        </div>

        <div class="admin-app-offer">
          <span class="admin-k">Предложение:</span>
          <p>{{ app.offer }}</p>
        </div>

        <footer class="admin-app-meta">
          <span>Подана: {{ app.submitted }}</span>
          <span v-if="app.status === 'approved'" class="meta-ok">Одобрена: {{ app.resolved }}</span>
          <span v-if="app.status === 'rejected'" class="meta-bad">
            Отклонена: {{ app.resolved }} ({{ app.reason }})
          </span>
        </footer>
      </article>
    </div>
  </div>
</template>
