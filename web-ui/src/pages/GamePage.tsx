import { useState } from 'react'
import { useWs, useWsListener } from '../hooks/useWs'
import { useToast } from '../components/Toast'

interface Server {
  name: string
  entityId: string
}

interface Role {
  name: string
}

type Step = 'search' | 'roles' | 'join'

export default function GamePage() {
  const { send } = useWs()
  const { toast } = useToast()

  const [step, setStep] = useState<Step>('search')
  const [keyword, setKeyword] = useState('')
  const [servers, setServers] = useState<Server[]>([])
  const [selectedServer, setSelectedServer] = useState<Server | null>(null)
  const [serverId, setServerId] = useState('')
  const [roles, setRoles] = useState<Role[]>([])
  const [newRoleName, setNewRoleName] = useState('')
  const [serverName, setServerName] = useState('')
  const [loading, setLoading] = useState(false)

  useWsListener((data) => {
    const t = data.type as string
    if (t === 'search_server_result') {
      setLoading(false)
      setServers((data.items as Server[]) || [])
    } else if (t === 'search_server_error') {
      setLoading(false)
      toast(`搜索失败: ${data.message}`, 'error')
    } else if (t === 'server_roles') {
      setLoading(false)
      const items = (data.items as Role[]) || []
      setRoles(items)
      setStep('roles')
    } else if (t === 'server_roles_error') {
      setLoading(false)
      toast(`获取角色失败: ${data.message}`, 'error')
    } else if (t === 'notlogin') {
      setLoading(false)
      toast('未登录，请先登录账号', 'error')
    } else if (t === 'channels_updated') {
      setLoading(false)
      toast('游戏已启动', 'success')
    } else if (t === 'start_error') {
      setLoading(false)
      toast(`启动失败: ${data.message}`, 'error')
    }
  })

  const searchServer = () => {
    if (!keyword) return
    setLoading(true)
    send({ type: 'search_server', keyword })
  }

  const selectServer = (s: Server) => {
    setSelectedServer(s)
    setServerId(s.entityId)
    setServerName(s.name)
    openServer(s.entityId)
  }

  const openServerManual = () => {
    if (!serverId) return
    setSelectedServer({ name: serverName || serverId, entityId: serverId })
    openServer(serverId)
  }

  const openServer = (id: string) => {
    setLoading(true)
    send({ type: 'open_server', serverId: id })
  }

  const createRole = () => {
    if (!newRoleName) return
    setLoading(true)
    send({ type: 'create_role_named', serverId, name: newRoleName })
    setNewRoleName('')
  }

  const joinGame = (role: string) => {
    setLoading(true)
    send({
      type: 'join_game',
      serverId,
      role,
      serverName: selectedServer?.name || '',
      socks5: { enabled: false },
    })
  }

  const reset = () => {
    setStep('search')
    setServers([])
    setSelectedServer(null)
    setRoles([])
    setServerId('')
    setServerName('')
  }

  return (
    <div className="max-w-lg">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-bold">加入游戏</h2>
        {step !== 'search' && (
          <button className="text-xs border border-zinc-200 px-2 py-1 rounded hover:bg-zinc-50" onClick={reset}>
            返回搜索
          </button>
        )}
      </div>

      {step === 'search' && (
        <>
          <div className="flex gap-2 mb-4">
            <input
              className="flex-1 border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
              placeholder="搜索服务器关键词"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && searchServer()}
            />
            <button
              className="bg-black text-white text-sm px-4 py-2 rounded hover:bg-zinc-800 disabled:opacity-50"
              onClick={searchServer}
              disabled={loading || !keyword}
            >
              {loading ? '搜索中...' : '搜索'}
            </button>
          </div>

          {servers.length > 0 && (
            <div className="flex flex-col gap-1 mb-6">
              {servers.map((s) => (
                <button
                  key={s.entityId}
                  className="text-left border border-zinc-200 rounded p-2 text-sm hover:bg-zinc-50"
                  onClick={() => selectServer(s)}
                >
                  {s.name} <span className="text-zinc-400">({s.entityId})</span>
                </button>
              ))}
            </div>
          )}

          <div className="border-t border-zinc-200 pt-4">
            <p className="text-xs text-zinc-400 mb-2">或手动输入服务器 ID</p>
            <div className="flex gap-2">
              <input
                className="flex-1 border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
                placeholder="服务器 ID"
                value={serverId}
                onChange={(e) => setServerId(e.target.value)}
              />
              <button
                className="border border-zinc-200 text-sm px-4 py-2 rounded hover:bg-zinc-50 disabled:opacity-50"
                onClick={openServerManual}
                disabled={loading || !serverId}
              >
                进入
              </button>
            </div>
          </div>
        </>
      )}

      {step === 'roles' && (
        <>
          <p className="text-sm text-zinc-400 mb-4">
            服务器: {selectedServer?.name || serverId}
          </p>

          {roles.length === 0 && (
            <p className="text-sm text-zinc-400 mb-4">该服务器没有角色</p>
          )}

          <div className="flex flex-col gap-1 mb-4">
            {roles.map((r) => (
              <button
                key={r.name}
                className="text-left border border-zinc-200 rounded p-2 text-sm hover:bg-zinc-50 disabled:opacity-50"
                onClick={() => joinGame(r.name)}
                disabled={loading}
              >
                {r.name}
              </button>
            ))}
          </div>

          {roles.length < 3 && (
            <div className="border-t border-zinc-200 pt-4">
              <p className="text-xs text-zinc-400 mb-2">创建新角色</p>
              <div className="flex gap-2">
                <input
                  className="flex-1 border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
                  placeholder="角色名称"
                  value={newRoleName}
                  onChange={(e) => setNewRoleName(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && createRole()}
                />
                <button
                  className="border border-zinc-200 text-sm px-4 py-2 rounded hover:bg-zinc-50 disabled:opacity-50"
                  onClick={createRole}
                  disabled={loading || !newRoleName}
                >
                  创建
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
