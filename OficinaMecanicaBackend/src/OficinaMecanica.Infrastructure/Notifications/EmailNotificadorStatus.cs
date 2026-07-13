using Microsoft.Extensions.Logging;
using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Domain.Entities;
using OficinaMecanica.Domain.Enums;
using OficinaMecanica.Infrastructure.Data;

namespace OficinaMecanica.Infrastructure.Notifications;

/// <summary>
/// Adapter simulado de notificação por e-mail. Em vez de enviar via SMTP, grava
/// a notificação numa tabela de outbox e registra um log — suficiente para
/// demonstrar o requisito "atualização de status via e-mail" de forma testável.
/// </summary>
public class EmailNotificadorStatus : INotificadorStatus
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmailNotificadorStatus> _logger;

    public EmailNotificadorStatus(AppDbContext db, ILogger<EmailNotificadorStatus> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task NotificarMudancaStatusAsync(
        OrdemServico ordem,
        StatusOrdemServico statusAnterior,
        StatusOrdemServico statusNovo,
        CancellationToken ct = default)
    {
        var email = ordem.Cliente?.Email;
        var nome = ordem.Cliente?.Nome ?? "cliente";
        var agora = DateTime.UtcNow;

        var assunto = $"OS {ordem.NumeroOS} — status atualizado para {statusNovo}";
        var corpo =
            $"Olá {nome}, a sua ordem de serviço {ordem.NumeroOS} mudou de " +
            $"'{statusAnterior}' para '{statusNovo}'.";

        var notificacao = new NotificacaoOutbox
        {
            OrdemServicoId = ordem.Id,
            NumeroOS = ordem.NumeroOS,
            DestinatarioEmail = email,
            StatusAnterior = statusAnterior,
            StatusNovo = statusNovo,
            Assunto = assunto,
            Corpo = corpo,
            Enviado = true,     // envio simulado: considerado entregue imediatamente
            DataCriacao = agora,
            DataEnvio = agora
        };

        _db.NotificacoesOutbox.Add(notificacao);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "E-mail (simulado) enviado para {Email} — OS {NumeroOS}: {Anterior} → {Novo}",
            string.IsNullOrWhiteSpace(email) ? "(cliente sem e-mail)" : email,
            ordem.NumeroOS, statusAnterior, statusNovo);
    }
}
