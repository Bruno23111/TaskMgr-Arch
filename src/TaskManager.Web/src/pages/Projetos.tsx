import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api, mensagemDeErro, type Pagina, type Projeto } from '../api'
import { useAuth } from '../auth'
import { Alerta, Carregando, Icone, Paginacao, Vazio } from '../components'

const TAMANHOS = [10, 20, 50, 100]

export default function Projetos() {
  const { ehGestor } = useAuth()
  const [params, setParams] = useSearchParams()
  const pagina = Number(params.get('pagina')) || 1
  const tamanho = Number(params.get('tamanho')) || 20
  const busca = params.get('busca') ?? ''

  const [dados, setDados] = useState<Pagina<Projeto> | null>(null)
  const [erro, setErro] = useState('')
  const [editando, setEditando] = useState<Projeto | null>(null)
  const [criando, setCriando] = useState(false)

  const carregar = useCallback(async () => {
    try {
      const resultado = await api<Pagina<Projeto>>(`/projetos?pagina=${pagina}&tamanho=${tamanho}`)
      setDados(resultado)
      // Troca o projeto em edição pela versão recarregada; a key do form muda e ele mostra os dados novos.
      setEditando((atual) => atual && (resultado.itens.find((x) => x.id === atual.id) ?? null))
    } catch (e) {
      setErro(mensagemDeErro(e))
    }
  }, [pagina, tamanho])

  useEffect(() => { carregar() }, [carregar])

  // Recarrega depois de toda ação, inclusive com erro, para que um 409 CONCORRENCIA traga a versão nova.
  async function executar(acao: () => Promise<unknown>) {
    setErro('')
    try {
      await acao()
    } catch (e) {
      setErro(mensagemDeErro(e))
    }
    await carregar()
  }

  function mudar(novos: Record<string, string | number>) {
    const proximo = new URLSearchParams(params)
    for (const [k, v] of Object.entries(novos)) {
      if (v === '' || (k === 'pagina' && v === 1)) proximo.delete(k)
      else proximo.set(k, String(v))
    }
    setParams(proximo, { replace: true })
  }

  function fecharForm() {
    setEditando(null)
    setCriando(false)
  }

  function salvar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const f = new FormData(e.currentTarget)
    const corpo = { nome: String(f.get('nome')).trim(), descricao: String(f.get('descricao')).trim() || null }
    executar(async () => {
      if (editando) await api(`/projetos/${editando.id}`, { method: 'PUT', body: { ...corpo, versao: editando.versao } })
      else await api('/projetos', { method: 'POST', body: corpo })
      fecharForm()
    })
  }

  function excluir(p: Projeto) {
    if (confirm(`Excluir o projeto "${p.nome}" e todas as suas tarefas?`))
      executar(() => api(`/projetos/${p.id}?versao=${p.versao}`, { method: 'DELETE' }))
  }

  // shortcut: a API não aceita filtro, então a busca vale só para a página carregada; mover para o servidor se o contrato ganhar ?nome=.
  const termo = busca.toLowerCase()
  const itens = (dados?.itens ?? []).filter((p) => `${p.nome} ${p.descricao ?? ''}`.toLowerCase().includes(termo))
  const formAberto = ehGestor && (criando || editando !== null)

  return (
    <section>
      <header className="pagina-topo">
        <div>
          <h1>Projetos</h1>
          <p className="subtitulo">Espaço compartilhado com todos os projetos da equipe.</p>
        </div>
        {ehGestor && !formAberto && (
          <button onClick={() => setCriando(true)}><Icone nome="mais" /> Novo projeto</button>
        )}
      </header>

      {erro && <Alerta mensagem={erro} onFechar={() => setErro('')} />}

      {formAberto && (
        <form className="card formulario" onSubmit={salvar} key={editando ? `${editando.id}-${editando.versao}` : 'novo'}>
          <h2>{editando ? 'Editar projeto' : 'Novo projeto'}</h2>
          <div className="campos">
            <label>
              Nome
              <input name="nome" maxLength={100} required autoFocus defaultValue={editando?.nome} />
            </label>
            <label>
              Descrição <span className="opcional">(opcional)</span>
              <input name="descricao" maxLength={255} defaultValue={editando?.descricao ?? ''} />
            </label>
          </div>
          <div className="formulario-acoes">
            <button type="button" className="secundario" onClick={fecharForm}>Cancelar</button>
            <button type="submit">{editando ? 'Salvar alterações' : 'Criar projeto'}</button>
          </div>
        </form>
      )}

      <div className="toolbar">
        <label className="busca">
          <Icone nome="busca" />
          <input type="search" value={busca} onChange={(e) => mudar({ busca: e.target.value })}
            placeholder="Buscar nesta página" aria-label="Buscar projetos nesta página" />
        </label>
        <label className="inline">
          Por página
          <select value={tamanho} onChange={(e) => mudar({ tamanho: e.target.value, pagina: 1 })}>
            {TAMANHOS.map((t) => <option key={t}>{t}</option>)}
          </select>
        </label>
      </div>

      <div className="card tabela-card">
        {!dados ? (
          <Carregando />
        ) : itens.length === 0 ? (
          busca ? (
            <Vazio icone="pasta" titulo="Nenhum projeto encontrado" texto={`Nada nesta página corresponde a "${busca}".`}>
              <button className="secundario" onClick={() => mudar({ busca: '' })}>Limpar busca</button>
            </Vazio>
          ) : (
            <Vazio icone="pasta" titulo="Nenhum projeto ainda"
              texto={ehGestor ? 'Crie o primeiro projeto para começar a organizar as tarefas.' : 'Quando um Gestor criar projetos, eles aparecem aqui.'}>
              {ehGestor && !formAberto && <button onClick={() => setCriando(true)}><Icone nome="mais" /> Novo projeto</button>}
            </Vazio>
          )
        ) : (
          <table>
            <thead>
              <tr><th>Projeto</th><th>Descrição</th>{ehGestor && <th className="col-acoes"><span className="sr-only">Ações</span></th>}</tr>
            </thead>
            <tbody>
              {itens.map((p) => (
                <tr key={p.id}>
                  <td data-label="Projeto">
                    <Link to={`/projetos/${p.id}`} className="link-projeto">
                      <span className="projeto-icone"><Icone nome="pasta" tamanho={16} /></span>
                      {p.nome}
                    </Link>
                  </td>
                  <td data-label="Descrição" className="texto-suave">{p.descricao || '—'}</td>
                  {ehGestor && (
                    <td className="acoes">
                      <button className="fantasma" onClick={() => { setCriando(false); setEditando(p) }}>
                        <Icone nome="editar" tamanho={16} /> Editar
                      </button>
                      <button className="fantasma perigo" onClick={() => excluir(p)}>
                        <Icone nome="excluir" tamanho={16} /> Excluir
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {dados && <Paginacao pagina={pagina} total={dados.total} tamanho={tamanho} rotulo={['projeto', 'projetos']} onMudar={(p) => mudar({ pagina: p })} />}
    </section>
  )
}
