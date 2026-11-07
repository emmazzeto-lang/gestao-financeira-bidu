using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Tests
{
    public class ReceitaServiceTests
    {
        private static Receita NovaReceita(int planoId = 1, int? fonteId = 1)
        {
            return new Receita
            {
                Descricao = "Repasse de teste",
                Valor = 5000,
                DataRecebimento = DateTime.UtcNow,
                ContaBancariaId = 1,
                PlanoTrabalhoId = planoId,
                FonteRecursoId = fonteId
            };
        }

        [Fact]
        public async Task ReceitaCorretaPassa()
        {
            using var context = BancoDeTeste.Criar();
            var service = new ReceitaService(context);

            var erro = await service.ValidarAsync(NovaReceita());

            Assert.Null(erro);
        }

        [Fact]
        public async Task SemPlanoValidadoNaoEntraDinheiro()
        {
            using var context = BancoDeTeste.Criar();
            var service = new ReceitaService(context);

            var erro = await service.ValidarAsync(NovaReceita(planoId: 2));

            Assert.Equal("A parceria só recebe aportes depois que o plano de trabalho for validado.", erro);
        }

        [Fact]
        public async Task ReceitaSemFonteEhRecusada()
        {
            using var context = BancoDeTeste.Criar();
            var service = new ReceitaService(context);

            var erro = await service.ValidarAsync(NovaReceita(fonteId: null));

            Assert.Equal("Informe a fonte de recurso da receita.", erro);
        }
    }
}