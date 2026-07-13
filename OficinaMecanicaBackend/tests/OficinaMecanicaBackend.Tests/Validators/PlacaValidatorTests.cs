using OficinaMecanica.Application.Validators;

namespace OficinaMecanicaBackend.Tests.Validators;

public class PlacaValidatorTests
{
    [Theory]
    // Formato antigo com traço
    [InlineData("ABC-1234", true)]
    // Formato antigo sem traço
    [InlineData("ABC1234", true)]
    // Formato Mercosul
    [InlineData("ABC1D23", true)]
    // Minúsculas (deve normalizar)
    [InlineData("abc-1234", true)]
    [InlineData("abc1d23", true)]
    // Inválidos
    [InlineData("AB1-2345", false)]
    [InlineData("ABCD123", false)]
    [InlineData("ABC12345", false)]
    [InlineData("ABC123", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValid_ReturnsExpected(string? placa, bool expected)
    {
        Assert.Equal(expected, PlacaValidator.IsValid(placa));
    }
}
