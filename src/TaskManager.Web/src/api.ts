// Cliente HTTP único: injeta o JWT e converte problem+json em ApiError (contrato em docs/openapi/taskmgr-v1.yaml).

export type Categoria = 'PENDENTE' | 'EM_ANDAMENTO' | 'CONCLUIDA'
export type Papel = 'Usuario' | 'Gestor'

export interface Projeto { id: string; nome: string; descricao: string | null; versao: number }
export interface Tarefa {
  id: string
  projetoId: string
  statusId: string
  responsavelId: string | null
  titulo: string
  descricao: string | null
  concluidaEm: string | null
  versao: number
}
export interface Status { id: string; codigo: string; nome: string; categoria: Categoria; protegido: boolean; versao: number }
export interface Responsavel { id: string; usuarioId: string; nome: string; email: string; ativo: boolean; papel: Papel; versao: number }
export interface Pagina<T> { itens: T[]; pagina: number; tamanho: number; total: number }

export class ApiError extends Error {
  status: number
  code?: string
  errors?: Record<string, string[]>

  constructor(status: number, problem: { code?: string; title?: string; detail?: string; errors?: Record<string, string[]> }) {
    super(problem.detail ?? problem.title ?? `Erro HTTP ${status}`)
    this.status = status
    this.code = problem.code
    this.errors = problem.errors
  }
}

let token: string | null = null
let aoExpirar = () => {}

export function configurarApi(novoToken: string | null, onUnauthorized: () => void) {
  token = novoToken
  aoExpirar = onUnauthorized
}

export async function api<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = {}
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  const res = await fetch(`/api${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  })

  // 401 com token = sessão expirada ou conta desativada; sem token é só login errado.
  if (res.status === 401 && token) aoExpirar()
  if (!res.ok) throw new ApiError(res.status, await res.json().catch(() => ({})))
  return (res.status === 204 ? undefined : await res.json()) as T
}

const MENSAGENS: Record<string, string> = {
  DADOS_INVALIDOS: 'Dados inválidos.',
  NAO_AUTENTICADO: 'Sessão inválida ou credenciais incorretas.',
  ACESSO_NEGADO: 'Você não tem permissão para esta ação.',
  RECURSO_NAO_ENCONTRADO: 'Registro não encontrado (pode ter sido excluído).',
  TRANSICAO_INVALIDA: 'Essa mudança de status não é permitida.',
  RESPONSAVEL_OBRIGATORIO: 'Atribua um responsável ativo antes de concluir a tarefa.',
  PROJETO_COM_TAREFA_EM_ANDAMENTO: 'O projeto tem tarefas em andamento e não pode ser excluído.',
  REGISTRO_EM_USO: 'Registro em uso por outras informações.',
  TAREFA_CONCLUIDA: 'Tarefa concluída: reabra antes de alterá-la.',
  REGISTRO_PROTEGIDO: 'Registro protegido.',
  RESPONSAVEL_INATIVO: 'O responsável está inativo.',
  VALOR_DUPLICADO: 'Já existe um registro com esse valor.',
  CONCORRENCIA: 'Outra pessoa alterou este registro. Os dados foram recarregados; tente de novo.',
  ERRO_INTERNO: 'Erro interno no servidor.',
}

export function mensagemDeErro(erro: unknown): string {
  if (!(erro instanceof ApiError)) return 'Não foi possível falar com a API.'
  const campos = Object.values(erro.errors ?? {}).flat()
  const base = (erro.code && MENSAGENS[erro.code]) || erro.message
  return campos.length ? `${base} ${campos.join(' ')}` : base
}
