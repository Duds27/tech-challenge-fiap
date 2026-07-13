namespace OficinaMecanica.Domain.Entities;

public class Servico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal PrecoBase { get; set; }
    public decimal? TempoEstimadoHoras { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public ICollection<ItemOrdemServico> ItensOrdemServico { get; set; } = new List<ItemOrdemServico>();
}
