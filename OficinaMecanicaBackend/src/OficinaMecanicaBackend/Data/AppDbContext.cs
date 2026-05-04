using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Models;

namespace OficinaMecanicaBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
