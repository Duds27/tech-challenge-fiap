using OficinaMecanica.Domain.Enums;

namespace OficinaMecanica.Domain.Entities;

public class OrdemServico
{
    public int Id { get; set; }
    public string NumeroOS { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public int VeiculoId { get; set; }
    public StatusOrdemServico Status { get; set; } = StatusOrdemServico.Recebida;
    public string? Descricao { get; set; }
    public decimal ValorTotal { get; set; }
    public bool OrcamentoAprovado { get; set; }
    public DateTime? DataAprovacaoOrcamento { get; set; }
    public DateTime? DataPrevisaoTermino { get; set; }
    public DateTime? DataInicioExecucao { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public DateTime? DataEntrega { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Veiculo Veiculo { get; set; } = null!;
    public ICollection<ItemOrdemServico> Itens { get; set; } = new List<ItemOrdemServico>();

    // ───────────────────────── Regras de domínio ─────────────────────────

    /// <summary>Valida a transição de status conforme o fluxo da OS.</summary>
    public (bool valid, string? error) ValidarTransicao(StatusOrdemServico novoStatus) =>
        (Status, novoStatus) switch
        {
            (StatusOrdemServico.Recebida, StatusOrdemServico.EmDiagnostico) => (true, null),
            (StatusOrdemServico.EmDiagnostico, StatusOrdemServico.AguardandoAprovacao) => (true, null),
            (StatusOrdemServico.AguardandoAprovacao, StatusOrdemServico.EmExecucao) when OrcamentoAprovado => (true, null),
            (StatusOrdemServico.AguardandoAprovacao, StatusOrdemServico.EmExecucao) => (false, "Orçamento não aprovado. Aprovação necessária antes de iniciar a execução."),
            (StatusOrdemServico.EmExecucao, StatusOrdemServico.Finalizada) => (true, null),
            (StatusOrdemServico.Finalizada, StatusOrdemServico.Entregue) => (true, null),
            _ => (false, $"Transição inválida: {Status} → {novoStatus}.")
        };

    /// <summary>Aplica o novo status e registra os marcos de data correspondentes.</summary>
    public void AplicarStatus(StatusOrdemServico novoStatus, DateTime agora)
    {
        Status = novoStatus;

        if (novoStatus == StatusOrdemServico.EmExecucao)
            DataInicioExecucao = agora;
        else if (novoStatus == StatusOrdemServico.Finalizada)
            DataFinalizacao = agora;
        else if (novoStatus == StatusOrdemServico.Entregue)
            DataEntrega = agora;

        DataAtualizacao = agora;
    }

    /// <summary>OS finalizadas ou entregues são omitidas da listagem operacional.</summary>
    public bool VisivelNaListagem =>
        Status != StatusOrdemServico.Finalizada && Status != StatusOrdemServico.Entregue;

    /// <summary>
    /// Prioridade de exibição na listagem: Em Execução &gt; Aguardando Aprovação &gt;
    /// Em Diagnóstico &gt; Recebida (menor número = maior prioridade).
    /// </summary>
    public static int PrioridadeListagem(StatusOrdemServico status) => status switch
    {
        StatusOrdemServico.EmExecucao => 0,
        StatusOrdemServico.AguardandoAprovacao => 1,
        StatusOrdemServico.EmDiagnostico => 2,
        StatusOrdemServico.Recebida => 3,
        _ => 4
    };
}
