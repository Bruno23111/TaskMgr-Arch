using System.ComponentModel.DataAnnotations;

namespace TaskManager.MVC.API.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Senha);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiraEm);
