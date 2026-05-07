using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanicaBackend.Data;

namespace OficinaMecanicaBackend.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestJwtKey      = "TestSecretKeyMustBe32CharsMinimum!!";
    private const string TestJwtIssuer   = "TestIssuer";
    private const string TestJwtAudience = "TestAudience";
    private const string AdminUsername   = "admin";
    private const string AdminPassword   = "admin123";

    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Inject test configuration (admin credentials, JWT settings, fake DB string)
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "server=test;database=test;user=test;password=test",
                ["Jwt:Key"]              = TestJwtKey,
                ["Jwt:Issuer"]          = TestJwtIssuer,
                ["Jwt:Audience"]        = TestJwtAudience,
                ["Jwt:AdminUsername"]   = AdminUsername,
                ["Jwt:AdminPassword"]   = AdminPassword,
                ["Jwt:ExpiresInMinutes"] = "60"
            }));

        builder.ConfigureServices(services =>
        {
            // Replace MySQL DbContext with SQLite in-memory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
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
