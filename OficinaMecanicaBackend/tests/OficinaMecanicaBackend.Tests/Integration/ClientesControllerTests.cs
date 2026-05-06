using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OficinaMecanicaBackend.Tests.Infrastructure;

namespace OficinaMecanicaBackend.Tests.Integration;

[Collection("Integration")]
public class ClientesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthorizeAsync()
    {
        var token = await CustomWebApplicationFactory.GetAuthTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Post_SemToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/clientes",
            new { CpfCnpj = "52998224725", Nome = "Sem Token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComToken_CpfValido_Returns201()
    {
        await AuthorizeAsync();

        var response = await _client.PostAsJsonAsync("/api/clientes",
            new { CpfCnpj = "529.982.247-25", Nome = "Cliente Válido" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComToken_CpfInvalido_Returns400()
    {
        await AuthorizeAsync();

        var response = await _client.PostAsJsonAsync("/api/clientes",
            new { CpfCnpj = "000.000.000-00", Nome = "CPF Inválido" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComToken_CpfDuplicado_Returns409()
    {
        await AuthorizeAsync();

        var payload = new { CpfCnpj = "111.444.777-35", Nome = "Duplicado" };
        await _client.PostAsJsonAsync("/api/clientes", payload);

        var response = await _client.PostAsJsonAsync("/api/clientes", payload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ComToken_Returns200()
    {
        await AuthorizeAsync();

        var response = await _client.GetAsync("/api/clientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Inexistente_Returns404()
    {
        await AuthorizeAsync();

        var response = await _client.GetAsync("/api/clientes/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ComOsExistente_Returns409()
    {
        await AuthorizeAsync();

        // Cria cliente
        var clienteResp = await _client.PostAsJsonAsync("/api/clientes",
            new { CpfCnpj = "407.302.170-27", Nome = "Com OS" });
        var clienteJson = await clienteResp.Content.ReadFromJsonAsync<IdResult>();

        // Cria veículo
        var veiculoResp = await _client.PostAsJsonAsync("/api/veiculos",
            new { Placa = "ZZZ9999", Marca = "Toyota", Modelo = "Corolla", Ano = 2021, ClienteId = clienteJson!.Id });
        var veiculoJson = await veiculoResp.Content.ReadFromJsonAsync<IdResult>();

        // Cria OS
        await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = clienteJson.Id, VeiculoId = veiculoJson!.Id });

        // Tenta excluir cliente com OS vinculada
        var response = await _client.DeleteAsync($"/api/clientes/{clienteJson.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private record IdResult(int Id);
}
