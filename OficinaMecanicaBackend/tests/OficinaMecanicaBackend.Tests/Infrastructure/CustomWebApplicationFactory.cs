using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MySql;
using Xunit;

namespace OficinaMecanicaBackend.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtKey      = "TestSecretKeyMustBe32CharsMinimum!!";
    private const string TestJwtIssuer   = "TestIssuer";
    private const string TestJwtAudience = "TestAudience";
    private const string AdminUsername   = "admin";
    private const string AdminPassword   = "admin123";

    // Sobe um MySQL real, na mesma versão usada em produção (docker-compose / Dockerfile),
    // para que os testes de integração exercitem o provedor Pomelo/MySQL e as migrations
    // exatamente como no ambiente real — em vez de um SQLite in-memory que diverge do alvo.
    private readonly MySqlContainer _mySql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("oficina_test")
        .WithUsername("oficina")
        .WithPassword("oficina_pwd")
        .Build();

    public Task InitializeAsync() => _mySql.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _mySql.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Injeta a connection string do container e as configurações de teste.
        // Como o provedor continua sendo MySQL (idêntico ao de produção), não é
        // preciso substituir o DbContext: o Program.cs registra o provedor correto
        // e aplica as migrations no startup (db.Database.Migrate()).
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _mySql.GetConnectionString(),
                ["Jwt:Key"]              = TestJwtKey,
                ["Jwt:Issuer"]          = TestJwtIssuer,
                ["Jwt:Audience"]        = TestJwtAudience,
                ["Jwt:AdminUsername"]   = AdminUsername,
                ["Jwt:AdminPassword"]   = AdminPassword,
                ["Jwt:ExpiresInMinutes"] = "60"
            }));

        builder.ConfigureServices(services =>
        {
            // Override JWT validation parameters so they match the test signing key.
            // Program.cs reads Jwt:Key at build-time (before ConfigureAppConfiguration
            // applies), so the middleware may have been configured with the appsettings
            // key. PostConfigure ensures we always validate with the test key at runtime.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.ASCII.GetBytes(TestJwtKey)),
                        ValidateIssuer   = true,
                        ValidIssuer      = TestJwtIssuer,
                        ValidateAudience = true,
                        ValidAudience    = TestJwtAudience,
                        ValidateLifetime = true,
                        ClockSkew        = TimeSpan.Zero
                    };
                });
        });
    }

    public static async Task<string> GetAuthTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Username = AdminUsername, Password = AdminPassword });
        response.EnsureSuccessStatusCode();

        using var doc = System.Text.Json.JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString()!;
    }
}
