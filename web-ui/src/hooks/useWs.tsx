import { createContext, useContext, useEffect, useRef, useCallback, useState, type ReactNode } from 'react'
import { wsClient, type Listener } from '../lib/wsClient'

type SendFn = (payload: Record<string, unknown>) => void
type RequestFn = (payload: Record<string, unknown>, responseType: string, timeoutMs?: number) => Promise<Record<string, unknown>>

interface WsContextValue {
  send: SendFn
  request: RequestFn
  onMessage: (listener: Listener) => () => void
  connected: boolean
}

const WsContext = createContext<WsContextValue | null>(null)

export function WsProvider({ children }: { children: ReactNode }) {
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    wsClient.connect()

    const check = setInterval(() => {
      setConnected(wsClient.connected)
    }, 500)

    return () => {
      clearInterval(check)
      wsClient.disconnect()
    }
  }, [])

  const send: SendFn = useCallback((payload) => {
    wsClient.send(payload)
  }, [])

  const request: RequestFn = useCallback((payload, responseType, timeoutMs = 15000) => {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        off()
        reject(new Error('timeout'))
      }, timeoutMs)

      const off = wsClient.onMessage((data) => {
        if (data.type === responseType || data.type === (payload.type as string) + '_error') {
          clearTimeout(timer)
          off()
          resolve(data)
        }
      })

      wsClient.send(payload)
    })
  }, [])

  const onMessage = useCallback((listener: Listener) => {
    return wsClient.onMessage(listener)
  }, [])

  return (
    <WsContext.Provider value={{ send, request, onMessage, connected }}>
      {children}
    </WsContext.Provider>
  )
}

export function useWs() {
  const ctx = useContext(WsContext)
  if (!ctx) throw new Error('useWs must be used within WsProvider')
  return ctx
}

export function useWsListener(listener: Listener) {
  const { onMessage } = useWs()
  const ref = useRef(listener)
  ref.current = listener

  useEffect(() => {
    return onMessage((data) => ref.current(data))
  }, [onMessage])
}
