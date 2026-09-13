using TaskManager.CleanArch.Application.Abstractions.Services;

namespace TaskManager.CleanArch.WebAPI.Services;

public sealed class RelogioSistema : IRelogio
{
    public DateTimeOffset DataHoraAgoraUTC => DateTimeOffset.UtcNow;
}
