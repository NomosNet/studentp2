import { computed, ref, watch } from 'vue'
import {
  createManagerAd,
  createPartnerAd,
  deleteManagerAd,
  deletePartnerAd,
  getAdCategories,
  getManagerAds,
  getPartnerAds,
  updateManagerAd,
  updatePartnerAd,
} from '../services/adsService'
import { useManagerCompany, consumeSelectionSkip } from './useManagerCompany'
import { useSession } from './useSession'

const discounts = ref([])
const categories = ref([])
const loadedKey = ref('')
let selectionWatchReady = false

function normalizeDiscount(payload) {
  const percentNumber = Number(payload.discount_percent ?? payload.percentNumber ?? payload.percent ?? 0)
  const safePercent = Number.isFinite(percentNumber) ? Math.max(0, Math.min(100, percentNumber)) : 0
  const cat = payload.categories?.[0] || payload.category || 'Прочее'

  return {
    id: String(payload.id),
    emoji: '🏷',
    title: String(payload.title || '').trim(),
    description: String(payload.description || '').trim(),
    percent: `-${safePercent}%`,
    percentNumber: safePercent,
    category: String(cat).trim() || 'Прочее',
    categoryIds: payload.category_ids || [],
    linkUrl: String(payload.url || payload.linkUrl || '').trim(),
    views: Number(payload.views ?? 0) || 0,
    clicks: Number(payload.clicks_count ?? payload.clicks ?? 0) || 0,
    address: String(payload.address || '').trim(),
    endDate: payload.end_date,
    raw: payload,
  }
}

function mapPayloadToApi(payload, categoryRef) {
  const categoryIdByName =
    categories.value.find((item) => item.name === String(categoryRef || payload.category || '').trim())?.id || null
  return {
    title: String(payload.title || '').trim(),
    description: String(payload.description || '').trim(),
    discount_percent: Number(payload.percentNumber ?? 0),
    url: String(payload.linkUrl || '').trim(),
    address: String(payload.address || 'Онлайн').trim(),
    end_date: payload.endDate || new Date(Date.now() + 1000 * 60 * 60 * 24 * 30).toISOString(),
    category_ids: Array.isArray(payload.categoryIds) && payload.categoryIds.length
      ? payload.categoryIds
      : [categoryIdByName || 1],
    emodzi_id: null,
    prioritet: 0,
  }
}

export function useManagerDiscounts() {
  const { user } = useSession()
  const { selectedPartnerId, isManager, loadCompanies } = useManagerCompany()

  function scopeKey() {
    if (isManager.value) return `m:${user.value?.email || ''}:${selectedPartnerId.value ?? ''}`
    return `p:${user.value?.email || ''}`
  }

  function requirePartnerId() {
    if (!isManager.value) return null
    if (!selectedPartnerId.value) {
      throw new Error('Нет закреплённой компании')
    }
    return selectedPartnerId.value
  }

  async function fetchAds() {
    if (isManager.value) {
      const partnerId = selectedPartnerId.value
      if (!partnerId) return { items: [] }
      return getManagerAds(partnerId, { limit: 100 })
    }
    return getPartnerAds({ limit: 100 })
  }

  async function refreshAds() {
    const [adsResponse, categoriesResponse] = await Promise.all([
      fetchAds(),
      categories.value.length ? Promise.resolve(categories.value) : getAdCategories(),
    ])
    const items = Array.isArray(adsResponse?.items) ? adsResponse.items : []
    discounts.value = items.map(normalizeDiscount)
    if (!categories.value.length) {
      categories.value = Array.isArray(categoriesResponse) ? categoriesResponse : []
    }
    loadedKey.value = scopeKey()
  }

  async function load() {
    await loadCompanies()
    const key = scopeKey()
    if (loadedKey.value === key) return
    await refreshAds()
  }

  if (!selectionWatchReady) {
    selectionWatchReady = true
    watch(selectedPartnerId, () => {
      if (consumeSelectionSkip() || user.value?.role !== 'manager') return
      loadedKey.value = ''
      void load()
    })
  }

  const items = computed(() => discounts.value)
  const categoryNames = computed(() => categories.value.map((item) => item.name))
  const totalClicks = computed(() => discounts.value.reduce((sum, item) => sum + item.clicks, 0))

  function getById(id) {
    return discounts.value.find((item) => item.id === String(id)) || null
  }

  async function createDiscount(payload) {
    const body = mapPayloadToApi(payload, payload.category)
    const partnerId = requirePartnerId()
    if (partnerId) await createManagerAd(partnerId, body)
    else await createPartnerAd(body)
    await refreshAds()
    return discounts.value[0] || null
  }

  async function updateDiscount(id, payload) {
    const idx = discounts.value.findIndex((item) => item.id === String(id))
    if (idx < 0) return null
    const base = discounts.value[idx]
    const body = mapPayloadToApi({ ...base, ...payload }, payload.category)
    const partnerId = requirePartnerId()
    if (partnerId) await updateManagerAd(partnerId, String(id), body)
    else await updatePartnerAd(String(id), body)
    await refreshAds()
    return discounts.value.find((item) => item.id === String(id)) || null
  }

  async function deleteDiscount(id) {
    const partnerId = requirePartnerId()
    if (partnerId) await deleteManagerAd(partnerId, String(id))
    else await deletePartnerAd(String(id))
    discounts.value = discounts.value.filter((item) => item.id !== String(id))
  }

  return {
    items,
    categoryNames,
    totalClicks,
    selectedPartnerId,
    isManager,
    load,
    getById,
    createDiscount,
    updateDiscount,
    deleteDiscount,
  }
}
