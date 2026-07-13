using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OficinaMecanicaBackend.Models.Enums;
using OficinaMecanicaBackend.Tests.Infrastructure;

namespace OficinaMecanicaBackend.Tests.Integration;

[Collection("Integration")]
public class OrdensServicoControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdensServicoControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthorizeAsync()
    {
        var token = await CustomWebApplicationFactory.GetAuthTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // Cria toda a base necessária e retorna os IDs
    private async Task<(int clienteId, int veiculoId, int servicoId, int pecaId)> SeedBaseAsync()
    {
        // Usa um CPF válido único para cada chamada
        var clienteResp = await _client.PostAsJsonAsync("/api/clientes",
            new { CpfCnpj = "529.982.247-25", Nome = "Cliente Base" });

        if (!clienteResp.IsSuccessStatusCode)
        {
            // Já existe — busca na lista
            var lista = await _client.GetFromJsonAsync<List<ClienteResult>>("/api/clientes");
            var existente = lista!.First(x => x.Nome == "Cliente Base");
            var veiculoR2 = await _client.PostAsJsonAsync("/api/veiculos",
                new { Placa = $"TES{Random.Shared.Next(1000, 9999)}", Marca = "Ford", Modelo = "Ka", Ano = 2020, ClienteId = existente.Id });
            var v2 = await veiculoR2.Content.ReadFromJsonAsync<IdResult>();
            var sResp2 = await _client.PostAsJsonAsync("/api/servicos",
                new { Nome = "Alinhamento", PrecoBase = 80m, Ativo = true });
            var s2 = await sResp2.Content.ReadFromJsonAsync<IdResult>();
            var pResp2 = await _client.PostAsJsonAsync("/api/pecas",
                new { Nome = "Óleo", PrecoUnitario = 30m, QuantidadeEstoque = 20, QuantidadeMinima = 2, Ativo = true });
            var p2 = await pResp2.Content.ReadFromJsonAsync<IdResult>();
            return (existente.Id, v2!.Id, s2!.Id, p2!.Id);
        }

        var cliente = await clienteResp.Content.ReadFromJsonAsync<IdResult>();

        var veiculoResp = await _client.PostAsJsonAsync("/api/veiculos",
            new { Placa = $"TES{Random.Shared.Next(1000, 9999)}", Marca = "Ford", Modelo = "Ka", Ano = 2020, ClienteId = cliente!.Id });
        var veiculo = await veiculoResp.Content.ReadFromJsonAsync<IdResult>();

        var servicoResp = await _client.PostAsJsonAsync("/api/servicos",
            new { Nome = "Alinhamento", PrecoBase = 80m, Ativo = true });
        var servico = await servicoResp.Content.ReadFromJsonAsync<IdResult>();

        var pecaResp = await _client.PostAsJsonAsync("/api/pecas",
            new { Nome = "Óleo", PrecoUnitario = 30m, QuantidadeEstoque = 20, QuantidadeMinima = 2, Ativo = true });
        var peca = await pecaResp.Content.ReadFromJsonAsync<IdResult>();

        return (cliente.Id, veiculo!.Id, servico!.Id, peca!.Id);
    }

    [Fact]
    public async Task GetStatus_SemToken_Returns200()
    {
        await AuthorizeAsync();
        var (cId, vId, _, _) = await SeedBaseAsync();

        var osResp = await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = cId, VeiculoId = vId });
        var os = await osResp.Content.ReadFromJsonAsync<IdResult>();

        // Remove o token para simular cliente não autenticado
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/ordens-servico/{os!.Id}/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TransicaoIlegal_Returns400()
    {
        await AuthorizeAsync();
        var (cId, vId, _, _) = await SeedBaseAsync();

        var osResp = await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = cId, VeiculoId = vId });
        var os = await osResp.Content.ReadFromJsonAsync<IdResult>();

        // Recebida → EmExecucao deve falhar
        var response = await _client.PutAsJsonAsync($"/api/ordens-servico/{os!.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.EmExecucao });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FluxoCompleto_AteEmExecucao_ComAprovacaoSemToken()
    {
        await AuthorizeAsync();
        var (cId, vId, _, _) = await SeedBaseAsync();

        // Cria OS
        var osResp = await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = cId, VeiculoId = vId });
        var os = await osResp.Content.ReadFromJsonAsync<IdResult>();

        // Recebida → EmDiagnostico
        await _client.PutAsJsonAsync($"/api/ordens-servico/{os!.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.EmDiagnostico });

        // EmDiagnostico → AguardandoAprovacao
        await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.AguardandoAprovacao });

        // Cliente aprova o orçamento (sem JWT)
        _client.DefaultRequestHeaders.Authorization = null;
        var aprovarResp = await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/aprovar-orcamento",
            new { Aprovado = true });
        Assert.Equal(HttpStatusCode.OK, aprovarResp.StatusCode);

        // Volta com token
        await AuthorizeAsync();

        // AguardandoAprovacao → EmExecucao (com orçamento aprovado)
        var execResp = await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.EmExecucao });
        Assert.Equal(HttpStatusCode.OK, execResp.StatusCode);
    }

    [Fact]
    public async Task AdicionarPeca_DecrementaEstoque()
    {
        await AuthorizeAsync();
        var (cId, vId, _, pecaId) = await SeedBaseAsync();

        // Verifica estoque inicial
        var pecaInicial = await _client.GetFromJsonAsync<PecaResult>($"/api/pecas/{pecaId}");
        var estoqueInicial = pecaInicial!.QuantidadeEstoque;

        var osResp = await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = cId, VeiculoId = vId });
        var os = await osResp.Content.ReadFromJsonAsync<IdResult>();

        await _client.PostAsJsonAsync($"/api/ordens-servico/{os!.Id}/itens",
            new { Tipo = (int)TipoItemOrdemServico.Peca, PecaId = pecaId, Quantidade = 2 });

        var pecaApos = await _client.GetFromJsonAsync<PecaResult>($"/api/pecas/{pecaId}");
        Assert.Equal(estoqueInicial - 2, pecaApos!.QuantidadeEstoque);
    }

    [Fact]
    public async Task RemoverPeca_RestaurarEstoque()
    {
        await AuthorizeAsync();
        var (cId, vId, _, pecaId) = await SeedBaseAsync();

        var osResp = await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = cId, VeiculoId = vId });
        var os = await osResp.Content.ReadFromJsonAsync<IdResult>();

        // Adiciona peça
        await _client.PostAsJsonAsync($"/api/ordens-servico/{os!.Id}/itens",
            new { Tipo = (int)TipoItemOrdemServico.Peca, PecaId = pecaId, Quantidade = 3 });

        var pecaApos = await _client.GetFromJsonAsync<PecaResult>($"/api/pecas/{pecaId}");
        var estoqueAposAdicao = pecaApos!.QuantidadeEstoque;

        // Busca o item para obter o ID
        var osDetalhes = await _client.GetFromJsonAsync<OrdemServicoResult>($"/api/ordens-servico/{os.Id}");
        var itemId = osDetalhes!.Itens.First().Id;

        // Remove o item
        await _client.DeleteAsync($"/api/ordens-servico/{os.Id}/itens/{itemId}");

        var pecaFinal = await _client.GetFromJsonAsync<PecaResult>($"/api/pecas/{pecaId}");
        Assert.Equal(estoqueAposAdicao + 3, pecaFinal!.QuantidadeEstoque);
    }

    [Fact]
    public async Task TempoMedioExecucao_Returns200ComPayload()
    {
        await AuthorizeAsync();

        var response = await _client.GetAsync("/api/ordens-servico/tempo-medio-execucao");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TempoMedioExecucaoResult>();
        Assert.NotNull(payload);
        Assert.True(payload!.OrdensConsideradas >= 0);
        Assert.False(string.IsNullOrEmpty(payload.TempoMedioFormatado));
    }

    [Fact]
    public async Task TempoMedioExecucao_ComOSFinalizada_ContabilizaOrdem()
    {
        await AuthorizeAsync();
        var (cId, vId, _, _) = await SeedBaseAsync();

        // Cria e leva a OS até Finalizada, passando por EmExecucao
        var osResp = await _client.PostAsJsonAsync("/api/ordens-servico",
            new { ClienteId = cId, VeiculoId = vId });
        var os = await osResp.Content.ReadFromJsonAsync<IdResult>();

        await _client.PutAsJsonAsync($"/api/ordens-servico/{os!.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.EmDiagnostico });
        await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.AguardandoAprovacao });
        await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/aprovar-orcamento",
            new { Aprovado = true });
        await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.EmExecucao });
        var finalizarResp = await _client.PutAsJsonAsync($"/api/ordens-servico/{os.Id}/status",
            new { NovoStatus = (int)StatusOrdemServico.Finalizada });
        Assert.Equal(HttpStatusCode.OK, finalizarResp.StatusCode);

        var payload = await _client.GetFromJsonAsync<TempoMedioExecucaoResult>(
            "/api/ordens-servico/tempo-medio-execucao");

        Assert.NotNull(payload);
        Assert.True(payload!.OrdensConsideradas >= 1);
    }

    // ── DTOs para deserialização ──
    private record IdResult(int Id);
    private record TempoMedioExecucaoResult(
        int OrdensConsideradas,
        double TempoMedioSegundos,
        double TempoMedioMinutos,
        double TempoMedioHoras,
        string TempoMedioFormatado);
    private record ClienteResult(int Id, string Nome);
    private record PecaResult(int Id, int QuantidadeEstoque);
    private record ItemResult(int Id);
    private record OrdemServicoResult(int Id, IEnumerable<ItemResult> Itens);
}
