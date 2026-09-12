using TaskManager.CleanArch.Application.DTOs;
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
}
