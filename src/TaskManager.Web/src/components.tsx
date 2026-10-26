import type { ReactNode } from 'react'

// Ícones no traço do Lucide (24x24, stroke), inline para não adicionar dependência.
const ICONES = {
  logo: <><path d="m9 11 3 3L22 4" /><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" /></>,
  busca: <><circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" /></>,
  mais: <><path d="M5 12h14" /><path d="M12 5v14" /></>,
  editar: <><path d="M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z" /><path d="m15 5 4 4" /></>,
  excluir: <><path d="M3 6h18" /><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" /><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" /></>,
  sair: <><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><path d="m16 17 5-5-5-5" /><path d="M21 12H9" /></>,
  anterior: <path d="m15 18-6-6 6-6" />,
  proxima: <path d="m9 18 6-6-6-6" />,
  pasta: <path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z" />,
  tarefas: <><rect width="8" height="4" x="8" y="2" rx="1" /><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" /><path d="M12 11h4" /><path d="M12 16h4" /><path d="M8 11h.01" /><path d="M8 16h.01" /></>,
  alerta: <><circle cx="12" cy="12" r="10" /><path d="M12 8v4" /><path d="M12 16h.01" /></>,
  fechar: <><path d="M18 6 6 18" /><path d="m6 6 12 12" /></>,
}

export function Icone({ nome, tamanho = 18 }: { nome: keyof typeof ICONES; tamanho?: number }) {
  return (
    <svg className="icone" width={tamanho} height={tamanho} viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {ICONES[nome]}
    </svg>
  )
}

export function Alerta({ mensagem, onFechar }: { mensagem: string; onFechar: () => void }) {
  return (
    <div className="alerta" role="alert">
      <Icone nome="alerta" />
      <span>{mensagem}</span>
      <button className="icone-botao" onClick={onFechar} aria-label="Fechar aviso"><Icone nome="fechar" /></button>
    </div>
  )
}

export function Vazio({ icone, titulo, texto, children }: { icone: 'pasta' | 'tarefas'; titulo: string; texto: string; children?: ReactNode }) {
  return (
    <div className="vazio">
      <span className="vazio-icone"><Icone nome={icone} tamanho={28} /></span>
      <h2>{titulo}</h2>
      <p>{texto}</p>
      {children}
    </div>
  )
}

export function Carregando({ linhas = 4 }: { linhas?: number }) {
  return (
    <div className="carregando" aria-busy="true" aria-label="Carregando">
      {Array.from({ length: linhas }, (_, i) => <div key={i} className="skeleton" />)}
    </div>
  )
}

export function Paginacao({ pagina, total, tamanho, rotulo, onMudar }: {
  pagina: number
  total: number
  tamanho: number
  rotulo: [singular: string, plural: string]
  onMudar: (pagina: number) => void
}) {
  const totalPaginas = Math.max(1, Math.ceil(total / tamanho))
  return (
    <nav className="paginacao" aria-label="Paginação">
      <span className="paginacao-info">{total} {total === 1 ? rotulo[0] : rotulo[1]}</span>
      <div className="paginacao-controles">
        <button className="secundario" disabled={pagina <= 1} onClick={() => onMudar(pagina - 1)}>
          <Icone nome="anterior" /> Anterior
        </button>
        <span aria-current="page">Página {pagina} de {totalPaginas}</span>
        <button className="secundario" disabled={pagina >= totalPaginas} onClick={() => onMudar(pagina + 1)}>
          Próxima <Icone nome="proxima" />
        </button>
      </div>
    </nav>
  )
}
