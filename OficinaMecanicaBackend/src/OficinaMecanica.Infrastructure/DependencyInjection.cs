using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Application.Abstractions;
using OficinaMecanica.Infrastructure.Data;
using OficinaMecanica.Infrastructure.Notifications;
using OficinaMecanica.Infrastructure.Persistence;
using OficinaMecanica.Infrastructure.Security;

namespace OficinaMecanica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 0)),
                mysql => mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IPecaRepository, PecaRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();

        services.AddScoped<INotificadorStatus, EmailNotificadorStatus>();
        services.AddSingleton<IAutenticador, JwtAutenticador>();

        return services;
    }
}
