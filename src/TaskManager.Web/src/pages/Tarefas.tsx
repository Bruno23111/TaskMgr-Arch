import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api, mensagemDeErro, type Categoria, type Pagina, type Projeto, type Responsavel, type Status, type Tarefa } from '../api'
import { useAuth } from '../auth'
import { Alerta, Carregando, Icone, Paginacao, Vazio } from '../components'
import { podeAlterarStatus, podeEditarConteudo, transicaoPermitida } from '../regras'

const TAMANHO = 50
const NOME_CATEGORIA: Record<Categoria, string> = { PENDENTE: 'Pendente', EM_ANDAMENTO: 'Em andamento', CONCLUIDA: 'Concluída' }

export default function Tarefas() {
  const { projetoId } = useParams()
  const { sessao, ehGestor } = useAuth()
  const [params, setParams] = useSearchParams()
  const pagina = Number(params.get('pagina')) || 1
  const filtroCategoria = params.get('categoria') ?? ''
  const filtroResponsavel = params.get('responsavel') ?? ''

  const [projeto, setProjeto] = useState<Projeto | null>(null)
  const [tarefas, setTarefas] = useState<Pagina<Tarefa> | null>(null)
  const [status, setStatus] = useState<Status[]>([])
  const [responsaveis, setResponsaveis] = useState<Responsavel[]>([])
  const [erro, setErro] = useState('')
  const [editando, setEditando] = useState<Tarefa | null>(null)
  const [criando, setCriando] = useState(false)

  const carregar = useCallback(async () => {
    try {
      const [p, t, s, r] = await Promise.all([
        api<Projeto>(`/projetos/${projetoId}`),
        api<Pagina<Tarefa>>(`/tarefas?projetoId=${projetoId}&pagina=${pagina}&tamanho=${TAMANHO}`),
        api<Status[]>('/status'),
        api<Responsavel[]>('/responsaveis'),
      ])
      setProjeto(p)
      setTarefas(t)
      // Troca a tarefa em edição pela versão recarregada; a key do form muda e ele mostra os dados novos.
      setEditando((atual) => atual && (t.itens.find((x) => x.id === atual.id) ?? null))
      setStatus(s)
      setResponsaveis(r)
    } catch (e) {
      setErro(mensagemDeErro(e))
    }
  }, [projetoId, pagina])

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

  function filtrar(chave: string, valor: string) {
    const proximo = new URLSearchParams(params)
    if (valor) proximo.set(chave, valor)
    else proximo.delete(chave)
    setParams(proximo, { replace: true })
  }

  const statusPorId = new Map(status.map((s) => [s.id, s]))
  const meuResponsavelId = responsaveis.find((r) => r.usuarioId === sessao?.usuarioId)?.id

  function fecharForm() {
    setEditando(null)
    setCriando(false)
  }

  function salvar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const f = new FormData(e.currentTarget)
    const corpo = { titulo: String(f.get('titulo')).trim(), descricao: String(f.get('descricao')).trim() || null }
    executar(async () => {
      if (editando) await api(`/tarefas/${editando.id}`, { method: 'PUT', body: { ...corpo, versao: editando.versao } })
      else await api('/tarefas', { method: 'POST', body: { ...corpo, projetoId } })
      fecharForm()
    })
  }

  function mudarStatus(t: Tarefa, statusId: string) {
    executar(() => api(`/tarefas/${t.id}/status`, { method: 'PATCH', body: { statusId, versao: t.versao } }))
  }

  function mudarResponsavel(t: Tarefa, responsavelId: string) {
    executar(() =>
      responsavelId
        ? api(`/tarefas/${t.id}/responsavel`, { method: 'PUT', body: { responsavelId, versao: t.versao } })
        : api(`/tarefas/${t.id}/responsavel?versao=${t.versao}`, { method: 'DELETE' }),
    )
  }

  function excluir(t: Tarefa) {
    if (confirm(`Excluir a tarefa "${t.titulo}"?`)) executar(() => api(`/tarefas/${t.id}?versao=${t.versao}`, { method: 'DELETE' }))
  }

  // shortcut: a API só filtra por projeto, então categoria e responsável filtram a página carregada; mover para o servidor se o contrato ganhar esses parâmetros.
  const daPagina = tarefas?.itens ?? []
  const porResponsavel = daPagina.filter(
    (t) => !filtroResponsavel || (filtroResponsavel === 'nenhum' ? t.responsavelId === null : t.responsavelId === filtroResponsavel),
  )
  const itens = porResponsavel.filter((t) => !filtroCategoria || statusPorId.get(t.statusId)?.categoria === filtroCategoria)
  const contagem = (c: Categoria) => porResponsavel.filter((t) => statusPorId.get(t.statusId)?.categoria === c).length
  const formAberto = criando || editando !== null

  return (
    <section>
      <nav className="trilha" aria-label="Trilha">
        <Link to="/projetos">Projetos</Link>
        <Icone nome="proxima" tamanho={14} />
        <span aria-current="page">{projeto?.nome ?? '…'}</span>
      </nav>

      <header className="pagina-topo">
        <div>
          <h1>{projeto?.nome ?? 'Projeto'}</h1>
          {projeto?.descricao && <p className="subtitulo">{projeto.descricao}</p>}
        </div>
        {ehGestor && !formAberto && (
          <button onClick={() => setCriando(true)}><Icone nome="mais" /> Nova tarefa</button>
        )}
      </header>

      {erro && <Alerta mensagem={erro} onFechar={() => setErro('')} />}

      {formAberto && (
        <form className="card formulario" onSubmit={salvar} key={editando ? `${editando.id}-${editando.versao}` : 'nova'}>
          <h2>{editando ? 'Editar tarefa' : 'Nova tarefa'}</h2>
          <div className="campos">
            <label>
              Título
              <input name="titulo" maxLength={100} required autoFocus defaultValue={editando?.titulo} />
            </label>
            <label>
              Descrição <span className="opcional">(opcional)</span>
              <input name="descricao" maxLength={255} defaultValue={editando?.descricao ?? ''} />
            </label>
          </div>
          <div className="formulario-acoes">
            <button type="button" className="secundario" onClick={fecharForm}>Cancelar</button>
            <button type="submit">{editando ? 'Salvar alterações' : 'Criar tarefa'}</button>
          </div>
        </form>
      )}

      <div className="toolbar">
        <div className="segmentos" role="group" aria-label="Filtrar por categoria">
          <button className={filtroCategoria === '' ? 'ativo' : ''} aria-pressed={filtroCategoria === ''} onClick={() => filtrar('categoria', '')}>
            Todas <span className="contador">{porResponsavel.length}</span>
          </button>
          {(Object.keys(NOME_CATEGORIA) as Categoria[]).map((c) => (
            <button key={c} className={filtroCategoria === c ? 'ativo' : ''} aria-pressed={filtroCategoria === c} onClick={() => filtrar('categoria', c)}>
              <span className={`ponto ${c}`} aria-hidden="true" />
              {NOME_CATEGORIA[c]} <span className="contador">{contagem(c)}</span>
            </button>
          ))}
        </div>
        <label className="inline">
          Responsável
          <select value={filtroResponsavel} onChange={(e) => filtrar('responsavel', e.target.value)}>
            <option value="">Todos</option>
            <option value="nenhum">Sem responsável</option>
            {responsaveis.map((r) => <option key={r.id} value={r.id}>{r.nome}</option>)}
          </select>
        </label>
      </div>

      <div className="card tabela-card">
        {!tarefas ? (
          <Carregando />
        ) : itens.length === 0 ? (
          daPagina.length === 0 ? (
            <Vazio icone="tarefas" titulo="Nenhuma tarefa neste projeto"
              texto={ehGestor ? 'Crie a primeira tarefa. Ela começa como Pendente.' : 'Quando um Gestor criar tarefas, elas aparecem aqui.'}>
              {ehGestor && !formAberto && <button onClick={() => setCriando(true)}><Icone nome="mais" /> Nova tarefa</button>}
            </Vazio>
          ) : (
            <Vazio icone="tarefas" titulo="Nenhuma tarefa com esses filtros" texto="Ajuste a categoria ou o responsável para ver outras tarefas.">
              <button className="secundario" onClick={() => setParams({}, { replace: true })}>Limpar filtros</button>
            </Vazio>
          )
        ) : (
          <table>
            <thead>
              <tr><th>Tarefa</th><th>Status</th><th>Responsável</th><th className="col-acoes"><span className="sr-only">Ações</span></th></tr>
            </thead>
            <tbody>
              {itens.map((t) => {
                const atual = statusPorId.get(t.statusId)
                const concluida = atual?.categoria === 'CONCLUIDA'
                const responsavel = responsaveis.find((r) => r.id === t.responsavelId)
                return (
                  <tr key={t.id}>
                    <td data-label="Tarefa">
                      <span className="tarefa-titulo">{t.titulo}</span>
                      {t.descricao && <span className="texto-suave bloco">{t.descricao}</span>}
                      {t.concluidaEm && (
                        <span className="texto-suave bloco pequeno">Concluída em {new Date(t.concluidaEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}</span>
                      )}
                    </td>
                    <td data-label="Status">
                      <select
                        aria-label={`Status de ${t.titulo}`}
                        className={`pilula ${atual?.categoria ?? ''}`}
                        value={t.statusId}
                        disabled={!atual || !podeAlterarStatus(t, ehGestor, meuResponsavelId)}
                        onChange={(e) => mudarStatus(t, e.target.value)}
                      >
                        {status
                          .filter((s) => s.id === t.statusId || (atual && transicaoPermitida(atual.categoria, s.categoria, ehGestor)))
                          .map((s) => <option key={s.id} value={s.id}>{s.nome}</option>)}
                      </select>
                    </td>
                    <td data-label="Responsável">
                      <div className="responsavel">
                        <span className={`avatar ${responsavel ? '' : 'vazio-avatar'}`} aria-hidden="true">
                          {responsavel ? responsavel.nome.trim().charAt(0).toUpperCase() : '?'}
                        </span>
                        <select
                          aria-label={`Responsável de ${t.titulo}`}
                          value={t.responsavelId ?? ''}
                          disabled={!ehGestor || concluida}
                          onChange={(e) => mudarResponsavel(t, e.target.value)}
                        >
                          <option value="">Sem responsável</option>
                          {responsaveis
                            .filter((r) => r.ativo || r.id === t.responsavelId)
                            .map((r) => <option key={r.id} value={r.id}>{r.nome}{r.ativo ? '' : ' (inativo)'}</option>)}
                        </select>
                      </div>
                    </td>
                    <td className="acoes">
                      {atual && podeEditarConteudo(t, atual.categoria, ehGestor, meuResponsavelId) && (
                        <button className="fantasma" onClick={() => { setCriando(false); setEditando(t) }}>
                          <Icone nome="editar" tamanho={16} /> Editar
                        </button>
                      )}
                      {ehGestor && !concluida && (
                        <button className="fantasma perigo" onClick={() => excluir(t)}>
                          <Icone nome="excluir" tamanho={16} /> Excluir
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>

      {tarefas && <Paginacao pagina={pagina} total={tarefas.total} tamanho={TAMANHO} rotulo={['tarefa', 'tarefas']} onMudar={(p) => filtrar('pagina', p > 1 ? String(p) : '')} />}
    </section>
  )
}
