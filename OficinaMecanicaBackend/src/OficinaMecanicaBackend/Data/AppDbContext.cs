using Microsoft.EntityFrameworkCore;
using OficinaMecanicaBackend.Models;

namespace OficinaMecanicaBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<ItemOrdemServico> ItensOrdemServico => Set<ItemOrdemServico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(e =>
        {
            e.Property(c => c.CpfCnpj).HasMaxLength(20).IsRequired();
            e.Property(c => c.Nome).HasMaxLength(200).IsRequired();
            e.Property(c => c.Email).HasMaxLength(150);
            e.Property(c => c.Telefone).HasMaxLength(20);
            e.HasIndex(c => c.CpfCnpj).IsUnique().HasDatabaseName("IX_Cliente_CpfCnpj");
        });

        modelBuilder.Entity<Veiculo>(e =>
        {
            e.Property(v => v.Placa).HasMaxLength(20).IsRequired();
            e.Property(v => v.Marca).HasMaxLength(100).IsRequired();
            e.Property(v => v.Modelo).HasMaxLength(100).IsRequired();
            e.HasIndex(v => v.Placa).IsUnique().HasDatabaseName("IX_Veiculo_Placa");
            e.HasOne(v => v.Cliente)
             .WithMany(c => c.Veiculos)
             .HasForeignKey(v => v.ClienteId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Peca>(e =>
        {
            e.Property(p => p.Nome).HasMaxLength(200).IsRequired();
            e.Property(p => p.Codigo).HasMaxLength(50);
            e.Property(p => p.PrecoUnitario).HasPrecision(18, 2);
            e.HasIndex(p => p.Codigo).IsUnique().HasDatabaseName("IX_Peca_Codigo");
        });

        modelBuilder.Entity<Servico>(e =>
        {
            e.Property(s => s.Nome).HasMaxLength(200).IsRequired();
            e.Property(s => s.PrecoBase).HasPrecision(18, 2);
            e.Property(s => s.TempoEstimadoHoras).HasPrecision(10, 2);
        });

        modelBuilder.Entity<OrdemServico>(e =>
        {
            e.ToTable("OrdensServico");
            e.Property(o => o.NumeroOS).HasMaxLength(50).IsRequired();
            e.Property(o => o.ValorTotal).HasPrecision(18, 2);
            e.Property(o => o.Status).HasConversion<int>();
            e.HasIndex(o => o.NumeroOS).IsUnique().HasDatabaseName("IX_OrdenServico_NumeroOS");
            e.HasOne(o => o.Cliente)
             .WithMany(c => c.OrdensServico)
             .HasForeignKey(o => o.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Veiculo)
             .WithMany(v => v.OrdensServico)
             .HasForeignKey(o => o.VeiculoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ItemOrdemServico>(e =>
        {
            e.ToTable("ItensOrdenServico");
            e.Property(i => i.OrdemServicoId).HasColumnName("OrdenServicoId");
            e.Property(i => i.Quantidade).HasPrecision(10, 2);
            e.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            e.Property(i => i.PrecoTotal).HasPrecision(18, 2);
            e.Property(i => i.Tipo).HasConversion<int>();
            e.HasOne(i => i.OrdemServico)
             .WithMany(o => o.Itens)
             .HasForeignKey(i => i.OrdemServicoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Servico)
             .WithMany(s => s.ItensOrdemServico)
             .HasForeignKey(i => i.ServicoId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.Peca)
             .WithMany(p => p.ItensOrdemServico)
             .HasForeignKey(i => i.PecaId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
