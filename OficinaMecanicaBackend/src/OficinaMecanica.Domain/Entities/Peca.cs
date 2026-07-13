namespace OficinaMecanica.Domain.Entities;

public class Peca
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Codigo { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public int QuantidadeMinima { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public ICollection<ItemOrdemServico> ItensOrdemServico { get; set; } = new List<ItemOrdemServico>();

    public bool EstoqueBaixo => QuantidadeEstoque < QuantidadeMinima;
}
