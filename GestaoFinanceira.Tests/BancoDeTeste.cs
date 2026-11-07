using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Tests
{
    // Cria um banco em memória novo para cada teste, já com um cadastro básico:
    // plano 1 validado (meta 1), plano 2 sem validação (meta 2),
    // fornecedor 1 ativo e fornecedor 2 inativo.
    public static class BancoDeTeste
    {
        public static AppDbContext Criar()
        {
            var opcoes = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(opcoes);

            context.Parcerias.Add(new Parceria { Id = 1, Nome = "Parceria de teste", TermoFomento = "TF 1/2026", ValorTotal = 10000 });
            context.FontesRecurso.Add(new FonteRecurso { Id = 1, Nome = "Recurso público municipal" });
            context.ContasBancarias.Add(new ContaBancaria { Id = 1, NomeBanco = "Banco de teste", Agencia = "1", NumeroConta = "1" });

            context.PlanosTrabalho.Add(new PlanoTrabalho { Id = 1, Descricao = "Plano validado", ValorAprovado = 5000, ParceriaId = 1, Validado = true });
            context.PlanosTrabalho.Add(new PlanoTrabalho { Id = 2, Descricao = "Plano sem validação", ValorAprovado = 5000, ParceriaId = 1 });

            context.Metas.Add(new Meta { Id = 1, Descricao = "Meta do plano validado", ValorPrevisto = 1000, PlanoTrabalhoId = 1, FonteRecursoId = 1 });
            context.Metas.Add(new Meta { Id = 2, Descricao = "Meta do plano sem validação", ValorPrevisto = 1000, PlanoTrabalhoId = 2, FonteRecursoId = 1 });

            context.Fornecedores.Add(new Fornecedor { Id = 1, Nome = "Fornecedor ativo", CpfCnpj = "11222333000181" });
            context.Fornecedores.Add(new Fornecedor { Id = 2, Nome = "Fornecedor inativo", CpfCnpj = "12345678000195", Ativo = false });

            context.SaveChanges();

            return context;
        }

        public static Despesa NovaDespesa(
            decimal valor = 100, int planoId = 1, int? metaId = 1, int? fornecedorId = null)
        {
            return new Despesa
            {
                Descricao = "Despesa de teste",
                Valor = valor,
                DataPagamento = DateTime.UtcNow,
                ContaBancariaId = 1,
                PlanoTrabalhoId = planoId,
                MetaId = metaId,
                FornecedorId = fornecedorId
            };
        }
    }
}