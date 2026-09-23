<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useAdminPartnerModals } from '../composables/useAdminPartnerModals'
import { createAdminManager, createAdminPartner, getAdminUsers, updateAdminManager } from '../services/adminService'

const { openMode, editingManager, close, notifyChanged } = useAdminPartnerModals()

const managerName = ref('')
const managerEmail = ref('')
const managerPhone = ref('')
const managerPassword = ref('')

const companyName = ref('')
const companyEmail = ref('')
const companyPassword = ref('')
const companyManagerId = ref('')
const managerOptions = ref([])
const error = ref('')
const submitting = ref(false)

const onKeydown = (e) => {
  if (e.key === 'Escape') close()
}

watch(openMode, async (mode) => {
  document.body.style.overflow = mode ? 'hidden' : ''
  error.value = ''
  if (!mode) return
  if (mode === 'manager') {
    managerName.value = ''
    managerEmail.value = ''
    managerPhone.value = ''
    managerPassword.value = ''
  } else if (mode === 'edit-manager') {
    managerName.value = editingManager.value?.name || ''
    managerPhone.value = editingManager.value?.phone === '—' ? '' : editingManager.value?.phone || ''
  } else {
    companyName.value = ''
    companyEmail.value = ''
    companyPassword.value = ''
    companyManagerId.value = ''
    try {
      const response = await getAdminUsers({ role: 'manager', limit: 100 })
      managerOptions.value = response?.items || []
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Не удалось загрузить менеджеров'
    }
  }
})

async function submitManager() {
  error.value = ''
  submitting.value = true
  try {
    if (openMode.value === 'edit-manager') {
      await updateAdminManager(editingManager.value.id, {
        full_name: managerName.value.trim(),
        phone: managerPhone.value.trim(),
      })
    } else {
      await createAdminManager({
        full_name: managerName.value.trim(),
        email: managerEmail.value.trim(),
        phone: managerPhone.value.trim(),
        password: managerPassword.value,
      })
    }
    notifyChanged()
    close()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось сохранить менеджера'
  } finally {
    submitting.value = false
  }
}

async function submitCompany() {
  error.value = ''
  submitting.value = true
  try {
    await createAdminPartner({
      company_name: companyName.value.trim(),
      email: companyEmail.value.trim(),
      password: companyPassword.value,
      manager_id: companyManagerId.value ? Number(companyManagerId.value) : null,
    })
    notifyChanged()
    close()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Не удалось создать компанию'
  } finally {
    submitting.value = false
  }
}

onMounted(() => window.addEventListener('keydown', onKeydown))
onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <div v-if="openMode" class="auth-overlay" role="presentation" @click.self="close">
      <div
        v-if="openMode === 'manager' || openMode === 'edit-manager'"
        class="auth-modal admin-partner-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="admin-add-manager-title"
      >
        <button type="button" class="auth-close" aria-label="Закрыть" @click="close">×</button>
        <h2 id="admin-add-manager-title" class="auth-title">
          {{ openMode === 'edit-manager' ? 'Редактировать менеджера' : 'Добавить менеджера' }}
        </h2>
        <p v-if="error" class="admin-modal-error">{{ error }}</p>

        <form class="auth-form" @submit.prevent="submitManager">
          <div class="auth-field">
            <label class="auth-label" for="adm-mgr-name">ФИО</label>
            <input id="adm-mgr-name" v-model="managerName" type="text" class="admin-modal-input" required autocomplete="name" />
          </div>
          <div v-if="openMode === 'manager'" class="auth-field">
            <label class="auth-label" for="adm-mgr-email">Email</label>
            <input id="adm-mgr-email" v-model="managerEmail" type="email" class="admin-modal-input" required autocomplete="email" />
          </div>
          <div class="auth-field">
            <label class="auth-label" for="adm-mgr-phone">Телефон</label>
            <input id="adm-mgr-phone" v-model="managerPhone" type="tel" class="admin-modal-input" required autocomplete="tel" />
          </div>
          <div v-if="openMode === 'manager'" class="auth-field">
            <label class="auth-label" for="adm-mgr-pass">Пароль</label>
            <input
              id="adm-mgr-pass"
              v-model="managerPassword"
              type="password"
              class="admin-modal-input"
              minlength="6"
              required
              autocomplete="new-password"
            />
          </div>
          <button type="submit" class="auth-submit" :disabled="submitting">
            {{ openMode === 'edit-manager' ? 'Сохранить' : 'Добавить' }}
          </button>
        </form>
      </div>

      <div
        v-else
        class="auth-modal admin-partner-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="admin-create-company-title"
      >
        <button type="button" class="auth-close" aria-label="Закрыть" @click="close">×</button>
        <h2 id="admin-create-company-title" class="auth-title">Создать компанию-партнера</h2>
        <p v-if="error" class="admin-modal-error">{{ error }}</p>

        <form class="auth-form" @submit.prevent="submitCompany">
          <div class="auth-field">
            <label class="auth-label" for="adm-co-name">Название компании</label>
            <input id="adm-co-name" v-model="companyName" type="text" class="admin-modal-input" required autocomplete="organization" />
          </div>
          <div class="auth-field">
            <label class="auth-label" for="adm-co-email">Email компании</label>
            <input id="adm-co-email" v-model="companyEmail" type="email" class="admin-modal-input" required autocomplete="email" />
          </div>
          <div class="auth-field">
            <label class="auth-label" for="adm-co-pass">Пароль</label>
            <input
              id="adm-co-pass"
              v-model="companyPassword"
              type="password"
              class="admin-modal-input"
              minlength="6"
              required
              autocomplete="new-password"
            />
          </div>
          <div class="auth-field">
            <label class="auth-label" for="adm-co-mgr">Назначить менеджера</label>
            <div class="admin-modal-select-wrap">
              <select id="adm-co-mgr" v-model="companyManagerId" class="admin-modal-select">
                <option value="">Не назначать</option>
                <option v-for="m in managerOptions" :key="m.id" :value="String(m.id)">{{ m.full_name || m.email }}</option>
              </select>
            </div>
          </div>
          <button type="submit" class="auth-submit" :disabled="submitting">Создать компанию</button>
        </form>
      </div>
    </div>
  </Teleport>
</template>
