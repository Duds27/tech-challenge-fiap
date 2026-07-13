namespace OficinaMecanica.Application.Abstractions;

/// <summary>
/// Lançada pela camada de infraestrutura quando um SaveChanges viola uma
/// restrição de unicidade (ex.: colisão de NumeroOS em condição de corrida),
/// mantendo a camada de aplicação independente de detalhes do provedor EF/DB.
/// </summary>
public class ConflitoPersistenciaException : Exception
{
    public ConflitoPersistenciaException(string message, Exception inner)
        : base(message, inner) { }
}
