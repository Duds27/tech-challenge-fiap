using OficinaMecanicaBackend.Validators;

namespace OficinaMecanicaBackend.Tests.Validators;

public class CpfCnpjValidatorTests
{
    // CPF válido com máscara
    [Theory]
    [InlineData("529.982.247-25", true)]
    // CPF válido sem máscara
    [InlineData("52998224725", true)]
    // CPF com todos os dígitos iguais
    [InlineData("111.111.111-11", false)]
    // CPF com dígitos verificadores errados
    [InlineData("123.456.789-00", false)]
    // CNPJ numérico válido
    [InlineData("00.000.000/0001-91", true)]
    // CNPJ alfanumérico válido (AB.CDE.FGH/0001-95 — calculado pelos pesos oficiais)
    [InlineData("AB.CDE.FGH/0001-95", true)]
    // CNPJ com todos os dígitos iguais
    [InlineData("11.111.111/1111-11", false)]
    // Entradas inválidas
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1234", false)]
    public void IsValid_ReturnsExpected(string? value, bool expected)
    {
        Assert.Equal(expected, CpfCnpjValidator.IsValid(value));
    }
}
