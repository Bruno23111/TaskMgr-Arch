namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtribuirResponsavelRequest(Guid ResponsavelId, int Versao);
