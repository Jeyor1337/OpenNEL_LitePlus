import { useWs } from '../hooks/useWs'

export default function AboutPage() {
  const { connected } = useWs()

  return (
    <div className="max-w-xl">
      <h2 className="text-lg font-bold mb-6">关于</h2>

      <div className="border border-zinc-200 rounded p-4 space-y-2 text-sm">
        <div>
          <span className="text-zinc-400">产品:</span> OpenNEL Lite Plus
        </div>
        <div>
          <span className="text-zinc-400">版本:</span> p1.0.0
        </div>
        <div>
          <span className="text-zinc-400">GitHub:</span>{' '}
          <a
            className="underline"
            href="https://github.com/Jeyor1337/OpenNEL_LitePlus"
            target="_blank"
            rel="noreferrer"
          >
            https://github.com/Jeyor1337/OpenNEL_LitePlus
          </a>
        </div>
        <div>
          <span className="text-zinc-400">QQ群:</span> 1071900403
        </div>
        <div className="pt-2 border-t border-zinc-200">
          <span className="text-zinc-400">WebSocket:</span>{' '}
          <span className={connected ? 'text-green-600' : 'text-red-600'}>
            {connected ? '已连接' : '未连接'}
          </span>
        </div>
      </div>
    </div>
  )
}
