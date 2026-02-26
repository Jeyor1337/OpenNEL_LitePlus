import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'

interface ToastItem {
  id: number
  message: string
  variant: 'success' | 'error' | 'info'
}

interface ToastCtx {
  toast: (message: string, variant?: ToastItem['variant']) => void
}

const Ctx = createContext<ToastCtx>({ toast: () => {} })

let _id = 0

export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([])

  const toast = useCallback((message: string, variant: ToastItem['variant'] = 'info') => {
    const id = ++_id
    setItems((prev) => [...prev, { id, message, variant }])
    setTimeout(() => setItems((prev) => prev.filter((t) => t.id !== id)), 3000)
  }, [])

  return (
    <Ctx.Provider value={{ toast }}>
      {children}
      <div className="fixed top-4 right-4 flex flex-col gap-2 z-50">
        {items.map((t) => (
          <div
            key={t.id}
            className={`px-4 py-2 text-sm rounded border ${
              t.variant === 'success'
                ? 'border-green-300 bg-green-50 text-green-800'
                : t.variant === 'error'
                  ? 'border-red-300 bg-red-50 text-red-800'
                  : 'border-zinc-300 bg-zinc-50 text-zinc-800'
            }`}
          >
            {t.message}
          </div>
        ))}
      </div>
    </Ctx.Provider>
  )
}

export function useToast() {
  return useContext(Ctx)
}

export default function Toast() {
  return null
}
