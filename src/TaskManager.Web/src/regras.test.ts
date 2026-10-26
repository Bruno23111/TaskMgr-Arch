import { describe, expect, it } from 'vitest'
import type { Categoria, Tarefa } from './api'
import { podeAlterarStatus, podeEditarConteudo, transicaoPermitida } from './regras'

const P: Categoria = 'PENDENTE', A: Categoria = 'EM_ANDAMENTO', C: Categoria = 'CONCLUIDA'

describe('transicaoPermitida', () => {
  // [de, para, Gestor, Usuário]
  it.each([
    [P, P, true, true], [P, A, true, true], [P, C, false, false],
    [A, P, true, true], [A, A, true, true], [A, C, true, true],
    [C, P, true, false], [C, A, false, false], [C, C, true, true],
  ] as const)('%s -> %s', (de, para, gestor, usuario) => {
    expect(transicaoPermitida(de, para, true)).toBe(gestor)
    expect(transicaoPermitida(de, para, false)).toBe(usuario)
  })
})

describe('permissões da tarefa', () => {
  const tarefa = (responsavelId: string | null): Tarefa =>
    ({ id: 't', projetoId: 'p', statusId: 's', responsavelId, titulo: 'x', descricao: null, concluidaEm: null, versao: 1 })

  it('Usuário só altera tarefa atribuída a si', () => {
    expect(podeAlterarStatus(tarefa('eu'), false, 'eu')).toBe(true)
    expect(podeAlterarStatus(tarefa('outro'), false, 'eu')).toBe(false)
    expect(podeAlterarStatus(tarefa(null), false, undefined)).toBe(false)
    expect(podeAlterarStatus(tarefa(null), true, undefined)).toBe(true)
  })

  it('ninguém edita conteúdo de tarefa concluída', () => {
    expect(podeEditarConteudo(tarefa('eu'), C, true, 'eu')).toBe(false)
    expect(podeEditarConteudo(tarefa('eu'), A, false, 'eu')).toBe(true)
  })
})
