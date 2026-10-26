import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { api, configurarApi } from './api'

interface Sessao { token: string; expiraEm: string; usuarioId: string; email: string; papeis: string[] }

interface AuthValue {
  sessao: Sessao | null
  ehGestor: boolean
  login: (email: string, senha: string) => Promise<void>
  logout: () => void
}

const CHAVE = 'taskmgr.sessao'
// JwtSecurityTokenHandler encurta ClaimTypes.Role para "role"; o nome longo cobre emissores sem esse mapeamento.
const ROLE_LONGO = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// Só lê o payload para a interface; quem valida assinatura e papel é a API.
function lerToken(token: string): Pick<Sessao, 'usuarioId' | 'email' | 'papeis'> {
  const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
  const payload = JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(base64), (c) => c.charCodeAt(0))))
  const papeis = payload.role ?? payload[ROLE_LONGO] ?? []
  return { usuarioId: payload.sub ?? payload.nameid, email: payload.email, papeis: Array.isArray(papeis) ? papeis : [papeis] }
}

function carregar(): Sessao | null {
  try {
    const sessao: Sessao | null = JSON.parse(localStorage.getItem(CHAVE) ?? 'null')
    return sessao && new Date(sessao.expiraEm) > new Date() ? sessao : null
  } catch {
    return null
  }
}

function salvar(sessao: Sessao | null) {
  try {
    if (sessao) localStorage.setItem(CHAVE, JSON.stringify(sessao))
    else localStorage.removeItem(CHAVE)
  } catch {
    // Sem storage a sessão vale só até recarregar a página.
  }
}

const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sessao, setSessao] = useState(carregar)

  const logout = useCallback(() => {
    salvar(null)
    setSessao(null)
  }, [])

  // Durante o render, para que os efeitos dos filhos já usem o token atual.
  configurarApi(sessao?.token ?? null, logout)

  useEffect(() => {
    if (!sessao) return
    const timer = setTimeout(logout, new Date(sessao.expiraEm).getTime() - Date.now())
    return () => clearTimeout(timer)
  }, [sessao, logout])

  const login = useCallback(async (email: string, senha: string) => {
    const { accessToken, expiraEm } = await api<{ accessToken: string; expiraEm: string }>('/auth/login', {
      method: 'POST',
      body: { email, senha },
    })
    const nova = { token: accessToken, expiraEm, ...lerToken(accessToken) }
    salvar(nova)
    setSessao(nova)
  }, [])

  const ehGestor = sessao?.papeis.includes('Gestor') ?? false
  return <AuthContext.Provider value={{ sessao, ehGestor, login, logout }}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth precisa estar dentro de <AuthProvider>')
  return ctx
}

export function RotaPrivada() {
  const { sessao } = useAuth()
  const location = useLocation()
  return sessao ? <Outlet /> : <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
}
