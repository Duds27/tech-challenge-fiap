namespace OficinaMecanica.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string CpfCnpj { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    /// <summary>
    /// Indica se o cliente está ativo. Clientes inativos não conseguem se
    /// autenticar (a Function Serverless de autenticação por CPF rejeita o login).
    /// </summary>
    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    public ICollection<OrdemServico> OrdensServico { get; set; } = new List<OrdemServico>();
}
