namespace OficinaMecanica.Application.Validators;

public static class CpfCnpjValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var cleaned = new string(value
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

        return cleaned.Length switch
        {
            11 => IsValidCpf(cleaned),
            14 => IsValidCnpj(cleaned),
            _ => false
        };
    }

    private static bool IsValidCpf(string cpf)
    {
        if (!cpf.All(char.IsDigit)) return false;
        if (cpf.Distinct().Count() == 1) return false;

        var digits = cpf.Select(c => c - '0').ToArray();

        int sum = 0;
        for (int i = 0; i < 9; i++) sum += digits[i] * (10 - i);
        int rem = sum % 11;
        int d1 = rem < 2 ? 0 : 11 - rem;
        if (digits[9] != d1) return false;

        sum = 0;
        for (int i = 0; i < 10; i++) sum += digits[i] * (11 - i);
        rem = sum % 11;
        int d2 = rem < 2 ? 0 : 11 - rem;
        return digits[10] == d2;
    }

    // Suporta o novo formato alfanumérico do CNPJ (vigente desde jan/2026).
    // Posições 0-11: A-Z ou 0-9 (uppercase); posições 12-13: somente dígitos.
    // Valor do char: dígito → c - '0'; letra → c - 'A' + 10.
    private static bool IsValidCnpj(string cnpj)
    {
        for (int i = 0; i < 12; i++)
        {
            if (!char.IsDigit(cnpj[i]) && !(cnpj[i] >= 'A' && cnpj[i] <= 'Z'))
                return false;
        }
        if (!char.IsDigit(cnpj[12]) || !char.IsDigit(cnpj[13])) return false;
        if (cnpj.Distinct().Count() == 1) return false;

        static int Val(char c) => char.IsDigit(c) ? c - '0' : c - 'A' + 10;

        int[] w1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] w2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int sum = 0;
        for (int i = 0; i < 12; i++) sum += Val(cnpj[i]) * w1[i];
        int rem = sum % 11;
        int d1 = rem < 2 ? 0 : 11 - rem;
        if (cnpj[12] - '0' != d1) return false;

        sum = 0;
        for (int i = 0; i < 13; i++) sum += Val(cnpj[i]) * w2[i];
        rem = sum % 11;
        int d2 = rem < 2 ? 0 : 11 - rem;
        return cnpj[13] - '0' == d2;
    }
}
