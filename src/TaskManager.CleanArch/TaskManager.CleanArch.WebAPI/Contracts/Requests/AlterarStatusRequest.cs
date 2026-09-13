namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AlterarStatusRequest(
    Guid StatusId, 
    int Versao
    );
