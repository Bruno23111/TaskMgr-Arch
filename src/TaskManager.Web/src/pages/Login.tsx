import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { mensagemDeErro } from '../api'
import { useAuth } from '../auth'
import { Icone } from '../components'

export default function Login() {
  const { sessao, login } = useAuth()
  const navigate = useNavigate()
  const destino = (useLocation().state as { from?: string } | null)?.from ?? '/projetos'
  const [erro, setErro] = useState('')
  const [enviando, setEnviando] = useState(false)

  if (sessao) return <Navigate to={destino} replace />

  async function entrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const dados = new FormData(e.currentTarget)
    setErro('')
    setEnviando(true)
    try {
      await login(String(dados.get('email')), String(dados.get('senha')))
      navigate(destino, { replace: true })
    } catch (err) {
      setErro(mensagemDeErro(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="login">
      <form className="card login-card" onSubmit={entrar}>
        <div className="login-cabecalho">
          <span className="marca-icone grande"><Icone nome="logo" tamanho={24} /></span>
          <h1>Entrar no TaskMgr</h1>
          <p className="subtitulo">Use a conta criada pelo Gestor da equipe.</p>
        </div>
        <label>
          E-mail
          <input name="email" type="email" autoComplete="username" required autoFocus />
        </label>
        <label>
          Senha
          <input name="senha" type="password" autoComplete="current-password" required />
        </label>
        {erro && (
          <p className="alerta" role="alert"><Icone nome="alerta" /> <span>{erro}</span></p>
        )}
        <button type="submit" className="largo" disabled={enviando}>{enviando ? 'Entrando…' : 'Entrar'}</button>
      </form>
    </main>
  )
}
