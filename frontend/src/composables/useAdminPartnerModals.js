import { ref } from 'vue'

const openMode = ref(null)
const editingManager = ref(null)
const revision = ref(0)

export function useAdminPartnerModals() {
  const openAddManager = () => {
    editingManager.value = null
    openMode.value = 'manager'
  }
  const openEditManager = (manager) => {
    editingManager.value = manager
    openMode.value = 'edit-manager'
  }
  const openCreateCompany = () => {
    openMode.value = 'company'
  }
  const close = () => {
    openMode.value = null
  }
  const notifyChanged = () => {
    revision.value += 1
  }

  return {
    openMode,
    editingManager,
    revision,
    openAddManager,
    openEditManager,
    openCreateCompany,
    close,
    notifyChanged,
  }
}
