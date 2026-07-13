using System.Text.RegularExpressions;

namespace OficinaMecanica.Application.Validators;

public static class PlacaValidator
{
    // ABC-1234 ou ABC1234
    private static readonly Regex OldFormat =
        new(@"^[A-Z]{3}[0-9]{4}$", RegexOptions.Compiled);

    // ABC1D23 (Mercosul)
    private static readonly Regex MercosulFormat =
        new(@"^[A-Z]{3}[0-9][A-Z][0-9]{2}$", RegexOptions.Compiled);

    public static bool IsValid(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return false;
        var normalized = placa.ToUpperInvariant().Replace("-", "");
        return OldFormat.IsMatch(normalized) || MercosulFormat.IsMatch(normalized);
    }
}
