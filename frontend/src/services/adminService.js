import { apiRequest } from './apiClient'

export function getPartnerRequests(params = {}) {
  return apiRequest('/api/v1/admin/partner-requests', { query: params })
}

export function approvePartnerRequest(userEmail) {
  return apiRequest(`/api/v1/admin/partner-requests/${encodeURIComponent(userEmail)}`, {
    method: 'POST',
  })
}

export function rejectPartnerRequest(userEmail, comment) {
  return apiRequest(`/api/v1/admin/partner-requests/${encodeURIComponent(userEmail)}/reject`, {
    method: 'POST',
    body: JSON.stringify({ comment }),
  })
}

export function getAdminSummary() {
  return apiRequest('/api/v1/admin/summary')
}

export function getAdminCompanies() {
  return apiRequest('/api/v1/admin/companies')
}

export function createAdminManager(payload) {
  return apiRequest('/api/v1/admin/managers', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateAdminManager(userId, payload) {
  return apiRequest(`/api/v1/admin/managers/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function assignManagerPartner(userId, partnerId) {
  return apiRequest(`/api/v1/admin/managers/${userId}/partners/${partnerId}`, { method: 'POST' })
}

export function unassignManagerPartner(userId, partnerId) {
  return apiRequest(`/api/v1/admin/managers/${userId}/partners/${partnerId}`, { method: 'DELETE' })
}

export function createAdminPartner(payload) {
  return apiRequest('/api/v1/admin/partners', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getAdminUsers(params = {}) {
  return apiRequest('/api/v1/admin/users', { query: params })
}

export function deleteAdminUser(userId) {
  return apiRequest(`/api/v1/admin/users/${userId}`, { method: 'DELETE' })
}
