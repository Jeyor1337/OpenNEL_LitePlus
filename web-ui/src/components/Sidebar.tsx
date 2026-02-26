import { NavLink } from 'react-router-dom'

const nav = [
  { to: '/login', label: '登录' },
  { to: '/accounts', label: '账号' },
  { to: '/game', label: '游戏' },
  { to: '/running', label: '运行中' },
  { to: '/settings', label: '设置' },
  { to: '/about', label: '关于' },
]

export default function Sidebar({ connected }: { connected: boolean }) {
  return (
    <aside className="w-48 border-r border-zinc-200 flex flex-col justify-between p-4">
      <div>
        <h1 className="text-sm font-bold mb-6">OpenNEL Lite+</h1>
        <nav className="flex flex-col gap-1">
          {nav.map((n) => (
            <NavLink
              key={n.to}
              to={n.to}
              className={({ isActive }) =>
                `px-3 py-1.5 text-sm rounded ${isActive ? 'bg-black text-white' : 'hover:bg-zinc-100'}`
              }
            >
              {n.label}
            </NavLink>
          ))}
        </nav>
      </div>
      <div className="flex items-center gap-2 text-xs text-zinc-400">
        <span className={`w-2 h-2 rounded-full ${connected ? 'bg-green-500' : 'bg-red-500'}`} />
        {connected ? '已连接' : '未连接'}
      </div>
    </aside>
  )
}
