using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        public DbSet<FonteRecurso> FontesRecurso { get; set; }
        public DbSet<Meta> Metas { get; set; }
        public DbSet<Fornecedor> Fornecedores { get; set; }
        public DbSet<Documento> Documentos { get; set; }
        public DbSet<MovimentoExtrato> MovimentosExtrato { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

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

            modelBuilder.Entity<Meta>()
                .Property(m => m.ValorPrevisto)
                .HasPrecision(18, 2);

            // Um plano tem várias metas; não deixa apagar um plano que tem metas
            modelBuilder.Entity<Meta>()
                .HasOne(m => m.PlanoTrabalho)
                .WithMany(p => p.Metas)
                .HasForeignKey(m => m.PlanoTrabalhoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Uma fonte paga várias metas; não deixa apagar uma fonte em uso
            modelBuilder.Entity<Meta>()
                .HasOne(m => m.FonteRecurso)
                .WithMany()
                .HasForeignKey(m => m.FonteRecursoId)
                .OnDelete(DeleteBehavior.Restrict);

            // O nome da fonte não pode se repetir
            modelBuilder.Entity<FonteRecurso>()
                .HasIndex(f => f.Nome)
                .IsUnique();
            // O CPF/CNPJ do fornecedor não pode se repetir
            modelBuilder.Entity<Fornecedor>()
                .HasIndex(f => f.CpfCnpj)
                .IsUnique();

            // Não deixa apagar um fornecedor que tem despesas
            modelBuilder.Entity<Despesa>()
                .HasOne(d => d.Fornecedor)
                .WithMany()
                .HasForeignKey(d => d.FornecedorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Não deixa apagar uma despesa que tem comprovantes
            modelBuilder.Entity<Documento>()
                .HasOne(d => d.Despesa)
                .WithMany()
                .HasForeignKey(d => d.DespesaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimentoExtrato>()
                .Property(m => m.Valor)
                .HasPrecision(18, 2);

            // O mesmo movimento do banco não entra duas vezes na mesma conta
            modelBuilder.Entity<MovimentoExtrato>()
                .HasIndex(m => new { m.ContaBancariaId, m.IdBanco })
                .IsUnique();

            // Cada despesa e cada receita é conciliada com um movimento só
            modelBuilder.Entity<MovimentoExtrato>()
                .HasIndex(m => m.DespesaId)
                .IsUnique();

            modelBuilder.Entity<MovimentoExtrato>()
                .HasIndex(m => m.ReceitaId)
                .IsUnique();

            // Não deixa apagar uma despesa ou receita já conciliada
            modelBuilder.Entity<MovimentoExtrato>()
                .HasOne(m => m.Despesa)
                .WithMany()
                .HasForeignKey(m => m.DespesaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimentoExtrato>()
                .HasOne(m => m.Receita)
                .WithMany()
                .HasForeignKey(m => m.ReceitaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Dois usuários não podem ter o mesmo e-mail
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();


                var conversorUtc = new ValueConverter<DateTime, DateTime>(
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                );

                var conversorUtcOpcional = new ValueConverter<DateTime?, DateTime?>(
                    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
                    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
                );

                foreach (var entidade in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var propriedade in entidade.GetProperties())
                    {
                        if (propriedade.ClrType == typeof(DateTime))

                        propriedade.SetValueConverter(conversorUtc);

                        else if (propriedade.ClrType == typeof(DateTime?))

                        propriedade.SetValueConverter(conversorUtcOpcional);
                    }
                }
        }
    }
}