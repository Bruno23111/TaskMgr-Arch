namespace TaskManager.CleanArch.WebAPI.Contracts.Responses;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiraEm);
