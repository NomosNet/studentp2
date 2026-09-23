<script setup>
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { computed, onMounted } from 'vue'
import { useManagerCompany } from '../composables/useManagerCompany'
import { useSession } from '../composables/useSession'

const router = useRouter()
const { user, logout } = useSession()
const { companies, selectedPartnerId, isManager, loadCompanies } = useManagerCompany()

const profileLabel = computed(() => user.value?.displayName || 'Компания')

function onCompanyChange(event) {
  const value = event.target.value
  selectedPartnerId.value = value === '' ? null : Number(value)
}

async function handleLogout() {
  await logout()
  router.push({ name: 'home' })
}

onMounted(() => {
  void loadCompanies()
})
</script>

<template>
  <div class="admin-layout mgr-layout">
    <header class="admin-top">
      <RouterLink :to="{ name: 'home' }" class="admin-brand">StudentPass</RouterLink>
      <nav class="admin-nav mgr-nav">
        <RouterLink :to="{ name: 'manager-discounts' }" exact-active-class="is-active">Мои скидки</RouterLink>
        <RouterLink :to="{ name: 'manager-statistics' }" exact-active-class="is-active">Статистика</RouterLink>
        <RouterLink :to="{ name: 'manager-discount-create' }" exact-active-class="is-active">Создать скидку</RouterLink>
      </nav>
      <div class="admin-profile">
        <label v-if="isManager" class="admin-company-switch">
          <span>Компания</span>
          <select :value="selectedPartnerId ?? ''" :disabled="!companies.length" @change="onCompanyChange">
            <option v-if="!companies.length" value="">Нет компаний</option>
            <option v-for="company in companies" :key="company.id" :value="company.id">
              {{ company.company_name }}
            </option>
          </select>
        </label>
        <span class="admin-profile-dot" aria-hidden="true" />
        <span class="admin-profile-name">{{ profileLabel }}</span>
        <button type="button" class="admin-logout" @click="handleLogout">Выйти</button>
      </div>
    </header>
    <main class="admin-body">
      <RouterView />
    </main>
  </div>
</template>
