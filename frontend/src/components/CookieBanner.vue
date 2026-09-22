<script setup>
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

const STORAGE_KEY = 'studentpass_cookie_consent'
const visible = ref(false)

onMounted(() => {
  visible.value = !localStorage.getItem(STORAGE_KEY)
})

function choose(value) {
  localStorage.setItem(STORAGE_KEY, value)
  visible.value = false
}
</script>

<template>
  <div v-if="visible" class="cookie-banner" role="dialog" aria-label="Уведомление о cookies">
    <p>
      StudentPass использует необходимую cookie <code>access_token</code> для входа в аккаунт.
      Подробности — в <RouterLink :to="{ name: 'cookies' }">политике cookies</RouterLink>.
    </p>
    <div class="cookie-banner__actions">
      <button type="button" class="sp-btn sp-btn--small" @click="choose('accepted')">Принять</button>
      <button type="button" class="sp-btn sp-btn--small sp-btn--ghost" @click="choose('necessary')">
        Только необходимые
      </button>
    </div>
  </div>
</template>
