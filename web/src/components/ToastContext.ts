import { createContext } from 'react'

export interface ToastContextValue {
  show: (text: string) => void
}

export const ToastContext = createContext<ToastContextValue | undefined>(undefined)
