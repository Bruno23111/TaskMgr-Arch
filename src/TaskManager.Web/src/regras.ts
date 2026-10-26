import type { Categoria, Tarefa } from './api'

// Matriz da seção 1 de docs/requisitos-funcionais.md. Só orienta a interface; a API continua sendo a autoridade.
const MATRIZ: Record<Categoria, Categoria[]> = {
  PENDENTE: ['PENDENTE', 'EM_ANDAMENTO'],
  EM_ANDAMENTO: ['PENDENTE', 'EM_ANDAMENTO', 'CONCLUIDA'],
  CONCLUIDA: ['PENDENTE', 'CONCLUIDA'],
}

export function transicaoPermitida(de: Categoria, para: Categoria, ehGestor: boolean): boolean {
  if (de === 'CONCLUIDA' && para === 'PENDENTE') return ehGestor // RN04: reabertura só por Gestor
  return MATRIZ[de].includes(para)
}

export function ehResponsavel(tarefa: Tarefa, meuResponsavelId: string | undefined): boolean {
  return tarefa.responsavelId !== null && tarefa.responsavelId === meuResponsavelId
}

export function podeAlterarStatus(tarefa: Tarefa, ehGestor: boolean, meuResponsavelId: string | undefined): boolean {
  return ehGestor || ehResponsavel(tarefa, meuResponsavelId)
}

export function podeEditarConteudo(tarefa: Tarefa, categoria: Categoria, ehGestor: boolean, meuResponsavelId: string | undefined): boolean {
  return categoria !== 'CONCLUIDA' && podeAlterarStatus(tarefa, ehGestor, meuResponsavelId)
}
