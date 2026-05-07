using OficinaMecanicaBackend.Models.Enums;

namespace OficinaMecanicaBackend.Models;

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
    public DateTime? DataFinalizacao { get; set; }
    public DateTime? DataEntrega { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Veiculo Veiculo { get; set; } = null!;
    public ICollection<ItemOrdemServico> Itens { get; set; } = new List<ItemOrdemServico>();
}
