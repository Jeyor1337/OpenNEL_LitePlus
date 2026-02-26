import { useState, useEffect } from 'react'
import { useWs, useWsListener } from '../hooks/useWs'
import { useToast } from '../components/Toast'

interface RunningGame {
  name: string
  role: string
  server: string
  local: string
}

export default function RunningGamesPage() {
  const { send } = useWs()
  const { toast } = useToast()
  const [games, setGames] = useState<RunningGame[]>([])
  const [loading, setLoading] = useState(false)

  const refresh = () => {
    setLoading(true)
    send({ type: 'get_running_games' })
  }

  useEffect(() => {
    refresh()
  }, [])

  useWsListener((data) => {
    const t = data.type as string
    if (t === 'running_games') {
      setLoading(false)
      setGames((data.items as RunningGame[]) || [])
    } else if (t === 'shutdown_ack') {
      toast('游戏已关闭', 'success')
      setTimeout(refresh, 500)
    } else if (t === 'shutdown_game_error') {
      toast(`关闭失败: ${data.message}`, 'error')
    }
  })

  const shutdown = (name: string) => {
    send({ type: 'shutdown_game', identifiers: [name] })
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-bold">运行中的游戏</h2>
        <button
          className="border border-zinc-200 text-sm px-3 py-1 rounded hover:bg-zinc-50"
          onClick={refresh}
          disabled={loading}
        >
          {loading ? '加载中...' : '刷新'}
        </button>
      </div>

      {games.length === 0 && !loading && (
        <p className="text-sm text-zinc-400">当前没有运行中的游戏进程</p>
      )}

      <div className="flex flex-col gap-2">
        {games.map((g) => (
          <div key={g.name} className="border border-zinc-200 rounded p-3 flex items-center justify-between">
            <div className="text-sm">
              <span className="font-medium">{g.role}</span>
              <span className="text-zinc-400 ml-2">@ {g.server}</span>
              {g.local && <span className="text-zinc-300 ml-2">({g.local})</span>}
            </div>
            <button
              className="text-xs border border-red-200 text-red-600 px-2 py-1 rounded hover:bg-red-50"
              onClick={() => shutdown(g.name)}
            >
              关闭
            </button>
          </div>
        ))}
      </div>
    </div>
  )
}
