import { reactive } from 'vue'

type ToastType = 'success' | 'error'

type ToastState = {
  visible: boolean
  message: string
  type: ToastType
}

const state = reactive<ToastState>({
  visible: false,
  message: '',
  type: 'success',
})

let timer: ReturnType<typeof setTimeout> | null = null

export function showToast(message: string, type: ToastType = 'success', duration = 2400) {
  state.message = message
  state.type = type
  state.visible = true

  if (timer) {
    clearTimeout(timer)
  }

  timer = setTimeout(() => {
    state.visible = false
  }, duration)
}

export function hideToast() {
  state.visible = false
  if (timer) {
    clearTimeout(timer)
    timer = null
  }
}

export function useToast() {
  return {
    toastState: state,
    showToast,
    hideToast,
  }
}
