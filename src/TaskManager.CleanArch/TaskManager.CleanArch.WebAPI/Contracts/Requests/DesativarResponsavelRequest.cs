using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record DesativarResponsavelRequest([property: Range(1, int.MaxValue)] int Versao);
