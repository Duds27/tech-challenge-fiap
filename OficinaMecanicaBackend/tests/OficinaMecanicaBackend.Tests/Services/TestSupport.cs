using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanicaBackend.Tests.Services;

/// <summary>Notificador de teste que apenas registra as chamadas, sem I/O.</summary>
internal sealed class FakeNotificador : INotificadorStatus
{
    public List<(StatusOrdemServico Anterior, StatusOrdemServico Novo)> Notificacoes { get; } = [];

    public Task NotificarMudancaStatusAsync(
        OrdemServico ordem, StatusOrdemServico statusAnterior, StatusOrdemServico statusNovo,
        CancellationToken ct = default)
    {
        Notificacoes.Add((statusAnterior, statusNovo));
        return Task.CompletedTask;
    }
}
