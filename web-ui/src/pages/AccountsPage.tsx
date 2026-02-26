import { useState, useEffect } from 'react'
import { useWs, useWsListener } from '../hooks/useWs'
import { useToast } from '../components/Toast'

interface Account {
  entityId: string
  channel: string
  status: string
}

export default function AccountsPage() {
  const { send } = useWs()
  const { toast } = useToast()
  const [accounts, setAccounts] = useState<Account[]>([])
  const [loading, setLoading] = useState(false)

  const refresh = () => {
    setLoading(true)
    send({ type: 'get_account' })
  }

  useEffect(() => {
    refresh()
  }, [])

  useWsListener((data) => {
    const t = data.type as string
    if (t === 'accounts') {
      setLoading(false)
      const items = (data.items as Account[]) || []
      setAccounts(items)
    } else if (t === 'notlogin') {
      setLoading(false)
      toast('未登录，请先登录账号', 'error')
    } else if (t === 'activate_account_error' || t === 'deactivate_account_error' || t === 'delete_account_error' || t === 'delete_error') {
      toast(`操作失败: ${data.message}`, 'error')
    }
  })

  const activate = (entityId: string) => {
    send({ type: 'activate_account', entityId })
    toast('正在激活...', 'info')
    setTimeout(refresh, 1500)
  }

  const deactivate = (entityId: string) => {
    send({ type: 'deactivate_account', entityId })
    toast('正在取消激活...', 'info')
    setTimeout(refresh, 1500)
  }

  const remove = (entityId: string) => {
    if (!confirm(`确认删除账号 ${entityId}?`)) return
    send({ type: 'delete_account', entityId })
    toast('正在删除...', 'info')
    setTimeout(refresh, 1500)
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-lg font-bold">账号列表</h2>
        <button
          className="border border-zinc-200 text-sm px-3 py-1 rounded hover:bg-zinc-50"
          onClick={refresh}
          disabled={loading}
        >
          {loading ? '加载中...' : '刷新'}
        </button>
      </div>

      {accounts.length === 0 && !loading && (
        <p className="text-sm text-zinc-400">暂无账号，请先登录</p>
      )}

      <div className="flex flex-col gap-2">
        {accounts.map((a) => (
          <div key={a.entityId} className="border border-zinc-200 rounded p-3 flex items-center justify-between">
            <div className="text-sm">
              <span className="font-medium">{a.entityId}</span>
              <span className="ml-3 text-zinc-400">渠道: {a.channel}</span>
              <span className={`ml-3 ${a.status === 'online' ? 'text-green-600' : 'text-zinc-400'}`}>
                {a.status}
              </span>
            </div>
            <div className="flex gap-2">
              {a.status === 'online' ? (
                <button
                  className="text-xs border border-zinc-200 px-2 py-1 rounded hover:bg-zinc-50"
                  onClick={() => deactivate(a.entityId)}
                >
                  取消激活
                </button>
              ) : (
                <button
                  className="text-xs border border-zinc-200 px-2 py-1 rounded hover:bg-zinc-50"
                  onClick={() => activate(a.entityId)}
                >
                  激活
                </button>
              )}
              <button
                className="text-xs border border-red-200 text-red-600 px-2 py-1 rounded hover:bg-red-50"
                onClick={() => remove(a.entityId)}
              >
                删除
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
