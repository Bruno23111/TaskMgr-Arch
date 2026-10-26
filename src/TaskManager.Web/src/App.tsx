import { BrowserRouter, Link, Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { AuthProvider, RotaPrivada, useAuth } from './auth'
import { Icone } from './components'
import Login from './pages/Login'
import Projetos from './pages/Projetos'
import Tarefas from './pages/Tarefas'

function Layout() {
  const { sessao, ehGestor, logout } = useAuth()
  return (
    <>
      <header className="topo">
        <Link to="/projetos" className="marca">
          <span className="marca-icone"><Icone nome="logo" /></span>
          TaskMgr
        </Link>
        <div className="usuario">
          <span className="avatar" aria-hidden="true">{sessao?.email.charAt(0).toUpperCase()}</span>
          <span className="usuario-info">
            <span className="usuario-email">{sessao?.email}</span>
            <span className={`papel ${ehGestor ? 'gestor' : ''}`}>{ehGestor ? 'Gestor' : 'Usuário'}</span>
          </span>
          <button className="fantasma" onClick={logout}><Icone nome="sair" tamanho={16} /> Sair</button>
        </div>
      </header>
      <main className="conteudo">
        <Outlet />
      </main>
    </>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route element={<RotaPrivada />}>
            <Route element={<Layout />}>
              <Route path="/projetos" element={<Projetos />} />
              <Route path="/projetos/:projetoId" element={<Tarefas />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/projetos" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
