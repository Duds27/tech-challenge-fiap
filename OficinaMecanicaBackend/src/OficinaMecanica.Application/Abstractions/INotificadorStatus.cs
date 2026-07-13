using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Application.Abstractions;

/// <summary>
/// Porta de saída para notificar o cliente sobre a mudança de status da OS
/// (ex.: por e-mail). A implementação de infraestrutura decide o meio real.
/// </summary>
public interface INotificadorStatus
{
    Task NotificarMudancaStatusAsync(
        OrdemServico ordem,
        StatusOrdemServico statusAnterior,
        StatusOrdemServico statusNovo,
        CancellationToken ct = default);
}
