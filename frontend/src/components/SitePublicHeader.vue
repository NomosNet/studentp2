<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AuthHeaderActions from './AuthHeaderActions.vue'

const route = useRoute()
const menuOpen = ref(false)

function closeMenu() {
  menuOpen.value = false
}

function toggleMenu() {
  menuOpen.value = !menuOpen.value
}

function onMobileAuthClick(event) {
  if (event.target.closest('button, a')) closeMenu()
}

function onKeydown(event) {
  if (event.key === 'Escape') closeMenu()
}

watch(
  () => route.fullPath,
  () => closeMenu(),
)

watch(menuOpen, (open) => {
  document.body.style.overflow = open ? 'hidden' : ''
})

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <header class="site-header" :class="{ 'is-open': menuOpen }">
    <div class="site-header__inner">
      <RouterLink :to="{ name: 'home' }" class="site-header__logo" @click="closeMenu">
        <span class="site-header__logo-mark" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none">
            <rect x="5" y="6.5" width="14" height="11" rx="2.6" stroke="currentColor" stroke-width="1.8" />
            <circle cx="8.7" cy="12" r="1.15" fill="currentColor" />
            <path
              d="M11.6 10.15h5.1M11.6 13.85h3.4"
              stroke="currentColor"
              stroke-width="1.7"
              stroke-linecap="round"
            />
          </svg>
        </span>
        <span class="site-header__logo-text">Student<span>Pass</span></span>
      </RouterLink>

      <nav class="site-header__nav" aria-label="Основное меню">
        <RouterLink :to="{ name: 'home' }" exact-active-class="is-active" active-class="">
          Главная
        </RouterLink>
        <RouterLink :to="{ name: 'catalog' }" active-class="is-active">Каталог скидок</RouterLink>
        <RouterLink :to="{ name: 'how' }" active-class="is-active">Как это работает</RouterLink>
      </nav>

      <div class="site-header__end">
        <div class="site-header__auth-desktop">
          <AuthHeaderActions />
        </div>
        <button
          type="button"
          class="site-header__burger"
          :class="{ 'is-open': menuOpen }"
          :aria-expanded="menuOpen"
          aria-controls="site-header-panel"
          :aria-label="menuOpen ? 'Закрыть меню' : 'Открыть меню'"
          @click="toggleMenu"
        >
          <span />
          <span />
          <span />
        </button>
      </div>
    </div>

    <div
      id="site-header-panel"
      class="site-header__panel"
      :inert="!menuOpen"
      :aria-hidden="menuOpen ? undefined : 'true'"
    >
      <div class="site-header__panel-inner">
        <nav class="site-header__mobile-nav" aria-label="Мобильное меню">
          <RouterLink :to="{ name: 'home' }" exact-active-class="is-active" active-class="" @click="closeMenu">
            Главная
          </RouterLink>
          <RouterLink :to="{ name: 'catalog' }" active-class="is-active" @click="closeMenu">
            Каталог скидок
          </RouterLink>
          <RouterLink :to="{ name: 'how' }" active-class="is-active" @click="closeMenu">
            Как это работает
          </RouterLink>
        </nav>
        <div class="site-header__auth-mobile" @click.capture="onMobileAuthClick">
          <AuthHeaderActions />
        </div>
      </div>
    </div>
  </header>
  <button
    v-if="menuOpen"
    type="button"
    class="site-header__backdrop"
    aria-label="Закрыть меню"
    @click="closeMenu"
  />
</template>
