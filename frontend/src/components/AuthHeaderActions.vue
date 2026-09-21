<script setup>
import { computed } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthModal } from '../composables/useAuthModal'
import { useSession } from '../composables/useSession'

const router = useRouter()
const { openRegister, openLogin } = useAuthModal()
const { user, isLoggedIn, isAdmin, isManager, logout } = useSession()

const initials = computed(() => {
  const name = String(user.value?.displayName || '').trim()
  if (!name) return 'SP'
  if (name.includes('@')) return name.slice(0, 2).toUpperCase()
  const parts = name.split(/\s+/).filter(Boolean)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
})

async function handleLogout() {
  const path = router.currentRoute.value.path
  const leaveCabinet = path.startsWith('/admin') || path.startsWith('/manager')
  await logout()
  if (leaveCabinet) router.push({ name: 'home' })
}
</script>

<template>
  <div v-if="!isLoggedIn" class="auth-header-actions">
    <button type="button" class="auth-header-btn auth-header-btn--ghost" @click="openLogin">
      Войти
    </button>
    <button type="button" class="auth-header-btn auth-header-btn--primary" @click="openRegister">
      Регистрация
    </button>
  </div>
  <div v-else class="auth-header-actions auth-header-actions--logged">
    <RouterLink
      v-if="isAdmin"
      class="auth-header-btn auth-header-btn--ghost"
      :to="{ name: 'admin-dashboard' }"
    >
      Админ-панель
    </RouterLink>
    <RouterLink
      v-if="isManager"
      class="auth-header-btn auth-header-btn--ghost"
      :to="{ name: 'manager-discounts' }"
    >
      Кабинет компании
    </RouterLink>
    <span class="auth-header-user" :title="user.email">
      <span class="auth-header-avatar" aria-hidden="true">{{ initials }}</span>
      <span class="auth-header-email">{{ user.displayName }}</span>
    </span>
    <button type="button" class="auth-header-btn auth-header-btn--ghost" @click="handleLogout">
      Выйти
    </button>
  </div>
</template>
