import { useState, useEffect } from 'react'
import { useWs, useWsListener } from '../hooks/useWs'
import { useToast } from '../components/Toast'

interface ProxyConfig {
  enabled: boolean
  address: string
  port: number
  username: string | null
  password: string | null
}

interface AdvancedConfig {
  lingQingGeApiKey: string | null
  crcSaltApiKey: string | null
}

export default function SettingsPage() {
  const { send } = useWs()
  const { toast } = useToast()

  const [proxy, setProxy] = useState<ProxyConfig>({
    enabled: false,
    address: '127.0.0.1',
    port: 1080,
    username: null,
    password: null,
  })
  const [advanced, setAdvanced] = useState<AdvancedConfig>({
    lingQingGeApiKey: null,
    crcSaltApiKey: null,
  })

  const load = () => {
    send({ type: 'get_proxy_config' })
    send({ type: 'get_advanced_config' })
  }

  useEffect(() => {
    load()
  }, [])

  useWsListener((data) => {
    const t = data.type as string
    if (t === 'proxy_config') {
      setProxy({
        enabled: !!data.enabled,
        address: (data.address as string) || '127.0.0.1',
        port: (data.port as number) || 1080,
        username: (data.username as string | null) ?? null,
        password: (data.password as string | null) ?? null,
      })
    } else if (t === 'advanced_config') {
      setAdvanced({
        lingQingGeApiKey: (data.lingQingGeApiKey as string | null) ?? null,
        crcSaltApiKey: (data.crcSaltApiKey as string | null) ?? null,
      })
    } else if (t === 'set_proxy_config_error' || t === 'set_advanced_config_error') {
      toast(`保存失败: ${data.message}`, 'error')
    }
  })

  const saveProxy = () => {
    send({ type: 'set_proxy_config', ...proxy })
    toast('代理设置已保存', 'success')
  }

  const saveAdvanced = () => {
    send({ type: 'set_advanced_config', ...advanced })
    toast('高级设置已保存', 'success')
  }

  return (
    <div className="max-w-2xl flex flex-col gap-8">
      <section>
        <h2 className="text-lg font-bold mb-4">代理设置</h2>
        <div className="border border-zinc-200 rounded p-4 flex flex-col gap-3">
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={proxy.enabled}
              onChange={(e) => setProxy((p) => ({ ...p, enabled: e.target.checked }))}
            />
            启用 SOCKS5 代理
          </label>

          <input
            className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
            placeholder="地址"
            value={proxy.address}
            onChange={(e) => setProxy((p) => ({ ...p, address: e.target.value }))}
          />

          <input
            className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
            placeholder="端口"
            type="number"
            value={proxy.port}
            onChange={(e) => setProxy((p) => ({ ...p, port: Number(e.target.value) || 0 }))}
          />

          <input
            className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
            placeholder="用户名（可选）"
            value={proxy.username || ''}
            onChange={(e) => setProxy((p) => ({ ...p, username: e.target.value || null }))}
          />

          <input
            className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
            placeholder="密码（可选）"
            type="password"
            value={proxy.password || ''}
            onChange={(e) => setProxy((p) => ({ ...p, password: e.target.value || null }))}
          />

          <div>
            <button className="bg-black text-white text-sm px-4 py-2 rounded hover:bg-zinc-800" onClick={saveProxy}>
              保存代理设置
            </button>
          </div>
        </div>
      </section>

      <section>
        <h2 className="text-lg font-bold mb-4">高级设置</h2>
        <div className="border border-zinc-200 rounded p-4 flex flex-col gap-3">
          <input
            className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
            placeholder="凌清阁 API Key（空为内置）"
            value={advanced.lingQingGeApiKey || ''}
            onChange={(e) =>
              setAdvanced((a) => ({ ...a, lingQingGeApiKey: e.target.value || null }))
            }
          />

          <input
            className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
            placeholder="CRC盐 API Key（空为内置）"
            value={advanced.crcSaltApiKey || ''}
            onChange={(e) =>
              setAdvanced((a) => ({ ...a, crcSaltApiKey: e.target.value || null }))
            }
          />

          <div>
            <button className="bg-black text-white text-sm px-4 py-2 rounded hover:bg-zinc-800" onClick={saveAdvanced}>
              保存高级设置
            </button>
          </div>
        </div>
      </section>
    </div>
  )
}
