import { Outlet } from 'react-router-dom'
import { useWs } from '../hooks/useWs'
import Toast from './Toast'
import Sidebar from './Sidebar'

export default function Layout() {
  const { connected } = useWs()

  return (
    <div className="flex h-screen">
      <Sidebar connected={connected} />
      <main className="flex-1 overflow-auto p-6">
        <Outlet />
      </main>
      <Toast />
    </div>
  )
}
