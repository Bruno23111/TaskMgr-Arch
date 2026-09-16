using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Common;

internal static class MapeamentosDto
{
    public static ProjetoDto ParaDto(this Projeto projeto) =>
        new(
            projeto.Id, 
            projeto.Nome, 
            projeto.Descricao, 
            projeto.Versao);

    public static TarefaDto ParaDto(this Tarefa tarefa) => 
        new(
            tarefa.Id,
            tarefa.ProjetoId,
            tarefa.StatusId, 
            tarefa.ResponsavelId, 
            tarefa.Titulo,
            tarefa.Descricao,
            tarefa.ConcluidaEm, 
            tarefa.Versao);

    public static StatusTarefaDto ParaDto(this StatusTarefa status) =>
        new(status.Id, status.Codigo, status.Nome, status.Categoria, status.Protegido, status.Versao);

    public static ResponsavelContaDto ParaDto(this Responsavel responsavel, ContaIdentityDto conta) =>
        new(
            responsavel.Id,
            responsavel.UsuarioId,
            responsavel.Nome,
            conta.Email,
            conta.Papeis.SingleOrDefault() ?? string.Empty,
            responsavel.Ativo && conta.Ativo,
            responsavel.Versao);
}
