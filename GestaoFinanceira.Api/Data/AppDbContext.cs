using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Models;

namespace GestaoFinanceira.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Parceria> Parcerias { get; set; }
        public DbSet<ContaBancaria> ContasBancarias { get; set; }
        public DbSet<PlanoTrabalho> PlanosTrabalho { get; set; }
        public DbSet<Receita> Receitas { get; set; }
        public DbSet<Despesa> Despesas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Parceria>()
                .Property(p => p.ValorTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ContaBancaria>()
                .Property(c => c.SaldoAtual)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PlanoTrabalho>()
                .Property(pt => pt.ValorAprovado)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Receita>()
                .Property(r => r.Valor)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<Despesa>()
                .Property(d => d.Valor)
                .HasPrecision(18, 2);
        }
    }
}