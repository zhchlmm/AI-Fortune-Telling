import axios from 'axios'

const AUTH_TOKEN_KEY = 'admin_token'
const ADMIN_USERNAME_KEY = 'admin_username'
const REQUIRE_PASSWORD_CHANGE_KEY = 'require_password_change'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5228/api/v1',
  timeout: 10000,
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      const requestUrl = error.config?.url ?? ''
      const isLoginRequest = requestUrl.includes('/auth/login')

      if (!isLoginRequest) {
        localStorage.removeItem(AUTH_TOKEN_KEY)
        localStorage.removeItem(ADMIN_USERNAME_KEY)
        localStorage.removeItem(REQUIRE_PASSWORD_CHANGE_KEY)

        if (window.location.pathname !== '/login') {
          window.location.href = '/login'
        }
      }
    }

    return Promise.reject(error)
  },
)

export type HealthResponse = {
  status: string
  timestamp: string
}

export type FortuneSessionDto = {
  id: string
  userId: string
  fortuneType: string
  inputSummary: string
  resultSummary: string
  createdAt: string
}

export type PagedResult<T> = {
  page: number
  pageSize: number
  total: number
  items: T[]
}

export type LoginResponse = {
  token: string
  expiresAt: string
  requirePasswordChange: boolean
}

export type FortuneTemplateDto = {
  id: string
  name: string
  fortuneType: string
  prompt: string
  isEnabled: boolean
  updatedAt: string
}

export type LoginAuditDto = {
  id: string
  username: string
  ipAddress: string
  isSuccess: boolean
  reason: string
  createdAt: string
}

export type AiAuditDto = {
  id: string
  fortuneType: string
  model: string
  degraded: boolean
  reason: string
  promptSource: string
  questionLength: number
  resultLength: number
  elapsedMs: number
  createdAt: string
}

export type AiAuditSummaryDto = {
  windowMinutes: number
  totalCalls: number
  degradedCalls: number
  degradedRate: number
  avgElapsedMs: number
  windowStartUtc: string
  windowEndUtc: string
}

export type AiAuditTypeDistributionDto = {
  fortuneType: string
  totalCalls: number
  degradedCalls: number
  degradedRate: number
  avgElapsedMs: number
}

export type ContentItemDto = {
  id: string
  title: string
  summary: string
  content: string
  categoryId?: string
  categoryName: string
  isPublished: boolean
  publishedAt: string
}

export type ContentCategoryDto = {
  id: string
  name: string
  sortOrder: number
  isEnabled: boolean
  updatedAt: string
}

export async function fetchHealth() {
  const { data } = await apiClient.get<HealthResponse>('/health')
  return data
}

export async function login(username: string, password: string) {
  try {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', {
      username,
      password,
    })
    return data
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const message = (error.response?.data as { message?: string } | undefined)?.message
      if (message) {
        throw new Error(message)
      }
    }

    throw error
  }
}

export async function changePassword(payload: {
  currentPassword: string
  newPassword: string
}) {
  await apiClient.post('/auth/change-password', payload)
}

export async function fetchPagedFortuneSessions(params: {
  userId?: string
  page: number
  pageSize: number
}) {
  const { data } = await apiClient.get<PagedResult<FortuneSessionDto>>('/admin/fortune-sessions', {
    params,
  })
  return data
}

export async function fetchPagedLoginAudits(params: {
  username?: string
  isSuccess?: boolean
  fromUtc?: string
  toUtc?: string
  page: number
  pageSize: number
}) {
  const { data } = await apiClient.get<PagedResult<LoginAuditDto>>('/admin/auth-audits', {
    params,
  })
  return data
}

export async function fetchPagedAiAudits(params: {
  fortuneType?: string
  degraded?: boolean
  fromUtc?: string
  toUtc?: string
  page: number
  pageSize: number
}) {
  const { data } = await apiClient.get<PagedResult<AiAuditDto>>('/admin/ai-audits', {
    params,
  })
  return data
}

export async function fetchAiAuditSummary(windowMinutes = 60) {
  const { data } = await apiClient.get<AiAuditSummaryDto>('/admin/ai-audits/summary', {
    params: { windowMinutes },
  })
  return data
}

export async function fetchAiAuditTypeDistribution(params: {
  fortuneType?: string
  degraded?: boolean
  fromUtc?: string
  toUtc?: string
}) {
  const { data } = await apiClient.get<AiAuditTypeDistributionDto[]>('/admin/ai-audits/type-distribution', {
    params,
  })
  return data
}

export async function exportLoginAuditsCsv(params: {
  username?: string
  isSuccess?: boolean
  fromUtc?: string
  toUtc?: string
}) {
  const { data } = await apiClient.get<Blob>('/admin/auth-audits/export', {
    params,
    responseType: 'blob',
  })
  return data
}

export async function fetchTemplates() {
  const { data } = await apiClient.get<FortuneTemplateDto[]>('/templates')
  return data
}

export async function fetchAdminContents(params?: {
  keyword?: string
  isPublished?: boolean
  categoryId?: string
}) {
  const { data } = await apiClient.get<ContentItemDto[]>('/admin/contents', {
    params,
  })
  return data
}

export async function fetchAdminContentDetail(id: string) {
  const { data } = await apiClient.get<ContentItemDto>(`/admin/contents/${id}`)
  return data
}

export async function createContent(payload: {
  title: string
  summary: string
  content: string
  categoryId: string
  isPublished: boolean
}) {
  const { data } = await apiClient.post<ContentItemDto>('/admin/contents', payload)
  return data
}

export async function updateContent(id: string, payload: {
  title: string
  summary: string
  content: string
  categoryId: string
  isPublished: boolean
}) {
  const { data } = await apiClient.put<ContentItemDto>(`/admin/contents/${id}`, payload)
  return data
}

export async function deleteContent(id: string) {
  await apiClient.delete(`/admin/contents/${id}`)
}

export async function fetchContentCategories() {
  const { data } = await apiClient.get<ContentCategoryDto[]>('/admin/content-categories')
  return data
}

export async function createContentCategory(payload: {
  name: string
  sortOrder: number
  isEnabled: boolean
}) {
  const { data } = await apiClient.post<ContentCategoryDto>('/admin/content-categories', payload)
  return data
}

export async function updateContentCategory(id: string, payload: {
  name: string
  sortOrder: number
  isEnabled: boolean
}) {
  const { data } = await apiClient.put<ContentCategoryDto>(`/admin/content-categories/${id}`, payload)
  return data
}

export async function deleteContentCategory(id: string) {
  await apiClient.delete(`/admin/content-categories/${id}`)
}

export async function createTemplate(payload: {
  name: string
  fortuneType: string
  prompt: string
  isEnabled: boolean
}) {
  const { data } = await apiClient.post<FortuneTemplateDto>('/templates', payload)
  return data
}

export async function updateTemplate(id: string, payload: {
  name: string
  fortuneType: string
  prompt: string
  isEnabled: boolean
}) {
  const { data } = await apiClient.put<FortuneTemplateDto>(`/templates/${id}`, payload)
  return data
}

export async function deleteTemplate(id: string) {
  await apiClient.delete(`/templates/${id}`)
}

export { AUTH_TOKEN_KEY }
