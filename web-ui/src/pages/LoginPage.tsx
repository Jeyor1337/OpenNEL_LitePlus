import { useState } from 'react'
import { useWs, useWsListener } from '../hooks/useWs'
import { useToast } from '../components/Toast'

export default function LoginPage() {
  const { send } = useWs()
  const { toast } = useToast()
  const [account, setAccount] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [freeLoading, setFreeLoading] = useState(false)

  useWsListener((data) => {
    const t = data.type as string
    if (t === 'Success_login') {
      setLoading(false)
      setFreeLoading(false)
      toast(`登录成功: ${data.entityId}`, 'success')
    } else if (t === 'login_4399_error') {
      setLoading(false)
      setFreeLoading(false)
      toast(`登录失败: ${data.message}`, 'error')
    } else if (t === 'get_free_account_result') {
      if (data.success) {
        toast(`获取小号成功: ${data.username}，正在登录...`, 'success')
        send({ type: 'login_4399', account: data.username, password: data.password })
      } else {
        setFreeLoading(false)
        toast(`获取小号失败: ${data.message}`, 'error')
      }
    } else if (t === 'get_free_account_status') {
      toast(data.message as string, 'info')
    } else if (t === 'get_free_account_error') {
      setFreeLoading(false)
      toast(`获取小号失败: ${data.message}`, 'error')
    } else if (t === 'get_free_account_requires_captcha') {
      setFreeLoading(false)
      toast('获取小号需要验证码，请改用手动登录', 'error')
    }
  })

  const handleLogin = () => {
    if (!account || !password) return
    setLoading(true)
    send({ type: 'login_4399', account, password })
  }

  const handleFreeAccount = () => {
    setFreeLoading(true)
    send({ type: 'get_free_account', source: 'random' })
  }

  return (
    <div className="max-w-md relative min-h-[calc(100vh-8rem)]">
      <h2 className="text-lg font-bold mb-6">4399 登录</h2>

      <div className="flex flex-col gap-3 mb-6">
        <input
          className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
          placeholder="账号"
          value={account}
          onChange={(e) => setAccount(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
        />
        <input
          className="border border-zinc-200 rounded px-3 py-2 text-sm outline-none focus:border-zinc-400"
          type="password"
          placeholder="密码"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleLogin()}
        />
        <button
          className="bg-black text-white text-sm px-4 py-2 rounded hover:bg-zinc-800 disabled:opacity-50"
          onClick={handleLogin}
          disabled={loading || !account || !password}
        >
          {loading ? '登录中...' : '登录'}
        </button>
      </div>

      <div className="border-t border-zinc-200 pt-4">
        <button
          className="border border-zinc-200 text-sm px-4 py-2 rounded hover:bg-zinc-50 disabled:opacity-50"
          onClick={handleFreeAccount}
          disabled={freeLoading}
        >
          {freeLoading ? '获取中...' : '随机获取小号'}
        </button>
      </div>

      <a
        href="https://fisproxy.org/"
        target="_blank"
        rel="noopener noreferrer"
        className="fixed bottom-6 right-6 text-xs text-zinc-400 hover:text-zinc-600 transition-colors"
      >
        最好的加速代理 FisProxy
      </a>
    </div>
  )
}
