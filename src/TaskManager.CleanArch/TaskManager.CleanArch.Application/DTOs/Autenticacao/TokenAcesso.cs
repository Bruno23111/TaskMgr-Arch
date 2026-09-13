namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record TokenAcesso(
    string Valor,
    DateTimeOffset ExpiraEm);
