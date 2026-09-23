import { computed, ref } from 'vue'
import { getManagerCompanies } from '../services/adsService'
import { useSession } from './useSession'

const companies = ref([])
const selectedPartnerId = ref(null)
const loadedFor = ref('')
let skipSelectionWatch = false

function selectPartner(id) {
  skipSelectionWatch = true
  selectedPartnerId.value = id
}

export function consumeSelectionSkip() {
  if (!skipSelectionWatch) return false
  skipSelectionWatch = false
  return true
}

export function useManagerCompany() {
  const { user } = useSession()
  const isManager = computed(() => user.value?.role === 'manager')
  const selectedCompany = computed(
    () => companies.value.find((item) => item.id === selectedPartnerId.value) || null,
  )

  async function loadCompanies() {
    if (!isManager.value) {
      companies.value = []
      selectedPartnerId.value = null
      loadedFor.value = ''
      return
    }

    const email = user.value?.email || ''
    if (loadedFor.value !== email) {
      const list = await getManagerCompanies()
      companies.value = Array.isArray(list) ? list : []
      loadedFor.value = email
    }

    const stillSelected = companies.value.some((item) => item.id === selectedPartnerId.value)
    if (!stillSelected) {
      selectPartner(companies.value[0]?.id ?? null)
    }
  }

  return {
    companies,
    selectedPartnerId,
    selectedCompany,
    isManager,
    loadCompanies,
  }
}
