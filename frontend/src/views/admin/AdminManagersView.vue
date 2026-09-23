<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useAdminPartnerModals } from '../../composables/useAdminPartnerModals'
import {
  assignManagerPartner,
  deleteAdminUser,
  getAdminCompanies,
  getAdminUsers,
  unassignManagerPartner,
} from '../../services/adminService'

const { openAddManager, openCreateCompany, openEditManager, revision } = useAdminPartnerModals()
const managers = ref([])
const companies = ref([])
const error = ref('')
const assignChoice = ref({})

const companiesUi = computed(() => companies.value)

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleDateString('ru-RU')
}

function formatCount(value) {
  return Number(value || 0).toLocaleString('ru-RU')
}

async function loadData() {
  error.value = ''
  const [usersResponse, companyRows] = await Promise.all([
    getAdminUsers({ role: 'manager', limit: 100 }),
    getAdminCompanies(),
  ])

  managers.value = (usersResponse?.items || []).map((user) => ({
    id: String(user.id),
    name: user.full_name || user.email,
    email: user.email,
    phone: user.phone || '—',
    assigned: formatDate(user.created_at),
    companies: (user.companies || []).map((company) => ({
      id: company.id,
      name: company.name,
      discounts: company.discounts || 0,
      clicks: company.clicks || 0,
    })),
  }))
  companies.value = Array.isArray(companyRows) ? companyRows : []
}

async function handleDeleteManager(userId) {
  if (!window.confirm('Удалить менеджера?')) return
  error.value = ''
  try {
    await deleteAdminUser(userId)
    await loadData()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось удалить менеджера'
  }
}

async function handleAssign(manager) {
  const partnerId = assignChoice.value[manager.id]
  if (!partnerId) return
  error.value = ''
  try {
    await assignManagerPartner(manager.id, partnerId)
    assignChoice.value[manager.id] = ''
    await loadData()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось закрепить компанию'
  }
}

async function handleUnassign(managerId, partnerId) {
  error.value = ''
  try {
    await unassignManagerPartner(managerId, partnerId)
    await loadData()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось снять компанию'
  }
}

watch(revision, () => {
  loadData().catch((err) => {
    error.value = err instanceof Error ? err.message : 'Не удалось обновить список'
  })
})

onMounted(() => {
  loadData().catch((err) => {
    error.value = err instanceof Error ? err.message : 'Не удалось загрузить менеджеров'
  })
})
</script>

<template>
  <div class="admin-page admin-container">
    <header class="admin-page-head admin-page-head--row">
      <div class="admin-page-title">
        <span class="admin-page-icon admin-page-icon--blue" aria-hidden="true" />
        <div>
          <h1>Менеджеры</h1>
          <p>Сотрудники платформы и закреплённые компании</p>
        </div>
      </div>
      <div class="admin-page-actions">
        <button type="button" class="admin-btn admin-btn--outline" @click="openCreateCompany">Создать компанию</button>
        <button type="button" class="admin-btn admin-btn--primary" @click="openAddManager">Добавить менеджера</button>
      </div>
    </header>

    <p v-if="error" class="admin-empty">{{ error }}</p>
    <p v-if="!managers.length" class="admin-empty">Менеджеров пока нет</p>

    <section class="admin-managers-list">
      <article v-for="m in managers" :key="m.id" class="admin-manager-card">
        <div class="admin-manager-card__top">
          <h2>{{ m.name }}</h2>
          <div class="admin-manager-tools">
            <button type="button" class="admin-icon-btn" aria-label="Редактировать" @click="openEditManager(m)">✎</button>
            <button
              type="button"
              class="admin-icon-btn admin-icon-btn--danger"
              aria-label="Удалить"
              @click="handleDeleteManager(m.id)"
            >
              🗑
            </button>
          </div>
        </div>
        <p class="admin-manager-meta">{{ m.email }} · {{ m.phone }}</p>
        <p class="admin-manager-meta">Создан: {{ m.assigned }}</p>
        <h3 class="admin-manager-sub">Закрепленные компании ({{ m.companies.length }}):</h3>
        <div class="admin-mini-cols">
          <div v-for="c in m.companies" :key="c.id" class="admin-mini-card">
            <span class="admin-mini-clicks">{{ formatCount(c.clicks) }}</span>
            <strong>{{ c.name }}</strong>
            <span>Скидок: {{ c.discounts }}</span>
            <button type="button" class="admin-mini-remove" @click="handleUnassign(m.id, c.id)">Снять</button>
          </div>
        </div>
        <form class="admin-assign" @submit.prevent="handleAssign(m)">
          <select v-model="assignChoice[m.id]" class="admin-assign-select">
            <option value="">Выберите компанию</option>
            <option v-for="company in companiesUi" :key="company.id" :value="String(company.id)">
              {{ company.company }}
            </option>
          </select>
          <button type="submit" class="admin-btn admin-btn--outline">Закрепить</button>
        </form>
      </article>
    </section>

    <section class="admin-panel">
      <div class="admin-panel-head">
        <h2>Партнерские компании</h2>
        <button type="button" class="admin-link-all admin-link-all--btn" @click="openCreateCompany">
          Создать компанию
        </button>
      </div>
      <div class="admin-table-wrap">
        <table class="admin-table">
          <thead>
            <tr>
              <th>Компания</th>
              <th>Email</th>
              <th>Менеджер</th>
              <th>Скидки</th>
              <th>Клики</th>
              <th>Статус</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!companiesUi.length">
              <td colspan="6">Компаний пока нет</td>
            </tr>
            <tr v-for="row in companiesUi" :key="row.id">
              <td>{{ row.company }}</td>
              <td>{{ row.email }}</td>
              <td>{{ row.manager }}</td>
              <td>{{ row.discounts }}</td>
              <td>{{ formatCount(row.clicks) }}</td>
              <td>
                <span class="admin-badge" :class="row.is_approved ? 'admin-badge--ok' : 'admin-badge--warn'">
                  {{ row.is_approved ? 'Активна' : 'Не одобрена' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
