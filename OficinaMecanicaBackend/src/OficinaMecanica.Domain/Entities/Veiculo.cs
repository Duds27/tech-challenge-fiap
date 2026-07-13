namespace OficinaMecanica.Domain.Entities;

public class Veiculo
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Ano { get; set; }
    public string? Cor { get; set; }
    public string? VinNumber { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public ICollection<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();
}
