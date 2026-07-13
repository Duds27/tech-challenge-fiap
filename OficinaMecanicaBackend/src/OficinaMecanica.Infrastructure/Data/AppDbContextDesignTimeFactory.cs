using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OficinaMecanica.Infrastructure.Data;

/// <summary>
/// Fábrica usada apenas pelas ferramentas do EF Core (dotnet ef) em tempo de
/// design, para gerar/aplicar migrations sem depender do host da API. A string
/// de conexão aqui não precisa apontar para um banco real ao criar migrations.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Port=3306;Database=OficinaMecanica;User=root;Password=Your_password123;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 0)),
                mysql => mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}
