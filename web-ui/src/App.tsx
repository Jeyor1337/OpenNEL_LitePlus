import { Routes, Route, Navigate } from 'react-router-dom'
import { WsProvider } from './hooks/useWs'
import { ToastProvider } from './components/Toast'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import AccountsPage from './pages/AccountsPage'
import GamePage from './pages/GamePage'
import RunningGamesPage from './pages/RunningGamesPage'
import SettingsPage from './pages/SettingsPage'
import AboutPage from './pages/AboutPage'

export default function App() {
  return (
    <WsProvider>
      <ToastProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<Navigate to="/login" replace />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/accounts" element={<AccountsPage />} />
            <Route path="/game" element={<GamePage />} />
            <Route path="/running" element={<RunningGamesPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/about" element={<AboutPage />} />
          </Route>
        </Routes>
      </ToastProvider>
    </WsProvider>
  )
}
