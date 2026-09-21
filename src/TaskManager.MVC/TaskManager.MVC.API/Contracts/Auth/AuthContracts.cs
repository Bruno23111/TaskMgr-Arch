namespace TaskManager.MVC.API.Contracts.Auth;

public sealed record LoginRequest(
    string Email,
    string Senha);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiraEm);
