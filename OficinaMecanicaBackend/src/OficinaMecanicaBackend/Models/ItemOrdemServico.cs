using OficinaMecanicaBackend.Models.Enums;

namespace OficinaMecanicaBackend.Models;

public class ItemOrdemServico
{
    public int Id { get; set; }
    public int OrdemServicoId { get; set; }
    public TipoItemOrdemServico Tipo { get; set; }
    public int? ServicoId { get; set; }
    public int? PecaId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal PrecoTotal { get; set; }
    public DateTime DataCriacao { get; set; }

    public OrdemServico OrdemServico { get; set; } = null!;
    public Servico? Servico { get; set; }
    public Peca? Peca { get; set; }
}
