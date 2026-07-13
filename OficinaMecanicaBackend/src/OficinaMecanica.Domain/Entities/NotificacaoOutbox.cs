using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Domain.Entities;

/// <summary>
/// Registro de notificação de mudança de status da OS (padrão outbox).
/// A notificação por e-mail é simulada: cada mudança de status gera um registro
/// aqui e um log, sem envio real via SMTP.
/// </summary>
public class NotificacaoOutbox
{
    public int Id { get; set; }
    public int OrdemServicoId { get; set; }
    public string NumeroOS { get; set; } = string.Empty;
    public string? DestinatarioEmail { get; set; }
    public StatusOrdemServico StatusAnterior { get; set; }
    public StatusOrdemServico StatusNovo { get; set; }
    public string Assunto { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public bool Enviado { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataEnvio { get; set; }
}
