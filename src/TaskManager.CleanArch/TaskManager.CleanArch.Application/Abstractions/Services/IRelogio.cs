namespace TaskManager.CleanArch.Application.Abstractions.Services;

public interface IRelogio
{
    DateTimeOffset DataHoraAgoraUTC { get; }
}
