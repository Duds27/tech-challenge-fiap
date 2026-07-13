using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OficinaMecanica.Application;
using OficinaMecanica.Application.Validators;
using OficinaMecanica.Infrastructure;
using OficinaMecanica.Infrastructure.Data;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // Clean Architecture: composição das camadas
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(connectionString);

    // JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key não configurado.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
    var jwtAudience = builder.Configuration["Jwt:Audience"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // Controllers + FluentValidation
    builder.Services.AddControllers();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateClienteValidator>();

    // Health checks (liveness sem dependências; readiness verifica o banco)
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("db", tags: new[] { "ready" });

    // Swagger with JWT support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "OficinaMecanica API", Version = "v1" });

        // Inclui os comentários XML (summaries) na documentação do Swagger.
        foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "OficinaMecanica*.xml"))
            c.IncludeXmlComments(xml);

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Cole apenas o token JWT (sem o prefixo 'Bearer ').",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational() && db.Database.ProviderName?.Contains("Sqlite") != true)
        {
            // O banco pode ainda estar subindo (ex.: MySQL em container na primeira
            // inicialização). Retenta a migração até o servidor aceitar conexões TCP,
            // em vez de derrubar a aplicação no primeiro erro transitório.
            const int maxAttempts = 12;
            const int delaySeconds = 5;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    db.Database.Migrate();
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    Log.Warning(ex,
                        "Banco de dados indisponível (tentativa {Attempt}/{Max}). Nova tentativa em {Delay}s...",
                        attempt, maxAttempts, delaySeconds);
                    Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
                }
            }
        }
        else
        {
            db.Database.EnsureCreated();
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Liveness: processo de pé (nenhum check dependente executa).
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
        .AllowAnonymous();
    // Readiness: pronto para receber tráfego (banco acessível).
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") })
        .AllowAnonymous();

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Expose for WebApplicationFactory in tests
public partial class Program { }
