using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Tests
{
    public class DespesaServiceTests
    {
        [Fact]
        public async Task DespesaCorretaPassa()
        {
            using var context = BancoDeTeste.Criar();
            var service = new DespesaService(context);

            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa());

            Assert.Null(erro);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public async Task ValorZeroOuNegativoEhRecusado(int valor)
        {
            using var context = BancoDeTeste.Criar();
            var service = new DespesaService(context);

            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(valor: valor));

            Assert.Equal("O valor da despesa deve ser maior que zero.", erro);
        }

        [Fact]
        public async Task PlanoSemValidacaoEhRecusado()
        {
            using var context = BancoDeTeste.Criar();
            var service = new DespesaService(context);

            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(planoId: 2, metaId: 2));

            Assert.Equal("O plano de trabalho ainda não foi validado.", erro);
        }

        [Fact]
        public async Task DespesaSemMetaEhRecusada()
        {
            using var context = BancoDeTeste.Criar();
            var service = new DespesaService(context);

            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(metaId: null));

            Assert.Equal("Informe a meta da despesa.", erro);
        }

        [Fact]
        public async Task MetaDeOutroPlanoEhRecusada()
        {
            using var context = BancoDeTeste.Criar();
            var service = new DespesaService(context);

            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(metaId: 2));

            Assert.Equal("A meta informada não pertence a este plano de trabalho.", erro);
        }

        [Fact]
        public async Task FornecedorInativoEhRecusado()
        {
            using var context = BancoDeTeste.Criar();
            var service = new DespesaService(context);

            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(fornecedorId: 2));

            Assert.Equal("O fornecedor informado não existe.", erro);
        }

        [Fact]
        public async Task ValorAcimaDoSaldoDaMetaEhRecusado()
        {
            using var context = BancoDeTeste.Criar();
            context.Despesas.Add(BancoDeTeste.NovaDespesa(valor: 900));
            await context.SaveChangesAsync();
            var service = new DespesaService(context);

            // A meta prevê 1.000 e já tem 900 gastos: sobram 100
            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(valor: 200));

            Assert.NotNull(erro);
            Assert.StartsWith("O valor passa o saldo da meta", erro);
        }

        [Fact]
        public async Task NaEdicaoAPropriaDespesaNaoContaNoSaldo()
        {
            using var context = BancoDeTeste.Criar();
            var existente = BancoDeTeste.NovaDespesa(valor: 900);
            context.Despesas.Add(existente);
            await context.SaveChangesAsync();
            var service = new DespesaService(context);

            // Aumentar de 900 para 1.000 cabe, porque os 900 antigos saem da conta
            var erro = await service.ValidarAsync(BancoDeTeste.NovaDespesa(valor: 1000), existente.Id);

            Assert.Null(erro);
        }
    }
}