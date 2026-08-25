import { useCallback, useRef, useState, type ReactNode } from 'react'
import { ToastContext } from './ToastContext'

interface ToastMessage {
  id: number
  text: string
}

const DISPLAY_MS = 3000

export function ToastProvider({ children }: { children: ReactNode }) {
  const [messages, setMessages] = useState<ToastMessage[]>([])
  const nextId = useRef(0)

  const show = useCallback((text: string) => {
    const id = nextId.current++
    setMessages((current) => [...current, { id, text }])
    setTimeout(() => setMessages((current) => current.filter((message) => message.id !== id)), DISPLAY_MS)
  }, [])

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 bottom-4 z-50 flex flex-col items-center gap-2">
        {messages.map((message) => (
          <div
            key={message.id}
            role="status"
            className="rounded-jm border border-success bg-bg-elevated px-4 py-2 text-sm text-fg shadow-jm-lg"
          >
            {message.text}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
