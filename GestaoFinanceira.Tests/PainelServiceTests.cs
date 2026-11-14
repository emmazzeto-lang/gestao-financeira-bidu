using System.Security.Claims;
using GestaoFinanceira.Api.Controllers;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Tests
{
    public class PainelServiceTests
    {
        private static DateTime Dia(int mes, int dia) => new(2026, mes, dia, 0, 0, 0, DateTimeKind.Utc);

        // Conta 1 abre com 1.000. Em agosto: despesa de 100.
        // Em setembro: receita de 500 (dia 10) e despesa de 200 (dia 12), esta com comprovante.
        private static AppDbContext BancoComMovimento()
        {
            var context = BancoDeTeste.Criar();
            context.ContasBancarias.Find(1)!.SaldoAtual = 1000;

            var agosto = BancoDeTeste.NovaDespesa(valor: 100);
            agosto.Id = 1;
            agosto.DataPagamento = Dia(8, 20);

            var setembro = BancoDeTeste.NovaDespesa(valor: 200);
            setembro.Id = 2;
            setembro.DataPagamento = Dia(9, 12);

            context.Despesas.AddRange(agosto, setembro);
            context.Receitas.Add(new Receita
            {
                Id = 1, Descricao = "Repasse", Valor = 500, DataRecebimento = Dia(9, 10),
                ContaBancariaId = 1, PlanoTrabalhoId = 1, FonteRecursoId = 1
            });
            context.Documentos.Add(new Documento
            {
                Tipo = "NF-e", Numero = "77", NomeOriginal = "n.pdf", NomeArquivo = "n.pdf",
                TipoConteudo = "application/pdf", DespesaId = 2
            });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task TransporteSomaOQueAconteceuAntesDoMes()
        {
            using var context = BancoComMovimento();

            var razao = await new PainelService(context).RazaoAsync(1, 2026, 9);

            // 1.000 de abertura - 100 de agosto
            Assert.Equal(900, razao!.Transporte);
            Assert.Equal(500, razao.Entradas);
            Assert.Equal(200, razao.Saidas);
            Assert.Equal(1200, razao.SaldoFinal);
        }

        [Fact]
        public async Task CadaLinhaTrazOSaldoDepoisDela()
        {
            using var context = BancoComMovimento();

            var razao = await new PainelService(context).RazaoAsync(1, 2026, 9);

            Assert.Equal(new[] { 1400m, 1200m }, razao!.Linhas.Select(l => l.Saldo));
            Assert.Equal("a-conciliar", razao.Linhas[1].Situacao);
            Assert.Contains("NF-e 77", razao.Linhas[1].Complemento);
        }

        [Fact]
        public async Task SemMesEscolhidoMostraOMaisRecente()
        {
            using var context = BancoComMovimento();

            var razao = await new PainelService(context).RazaoAsync(1, null, null);

            Assert.Equal(9, razao!.Mes);
        }

        [Fact]
        public async Task ContadoresDoMenu()
        {
            using var context = BancoComMovimento();
            context.MovimentosExtrato.Add(new MovimentoExtrato
            {
                Data = Dia(9, 12), Valor = -200, Descricao = "PIX", IdBanco = "A1", ContaBancariaId = 1
            });
            context.SaveChanges();

            var contadores = await new PainelService(context).ContadoresAsync();

            // A despesa de agosto não tem comprovante; o PIX ainda não foi conciliado
            Assert.Equal(1, contadores.SemComprovante);
            Assert.Equal(1, contadores.AConciliar);
        }

        [Fact]
        public async Task ReceitaGuardaQuemLancou()
        {
            using var context = BancoDeTeste.Criar();
            var controller = new ReceitasController(context, new ReceitaService(context));

            // Simula a pessoa logada: o mesmo que o cookie faria numa chamada de verdade
            var usuario = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "teste"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } };

            await controller.PostReceita(new Receita
            {
                Descricao = "Repasse", Valor = 100, DataRecebimento = DateTime.UtcNow,
                ContaBancariaId = 1, PlanoTrabalhoId = 1, FonteRecursoId = 1
            });

            var gravada = context.Receitas.Single();
            Assert.Equal(7, gravada.CriadoPorId);
            Assert.NotNull(gravada.CriadoEm);
        }

        [Fact]
        public async Task ComandoDeRecuperacaoTrocaASenhaEReativa()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            var (usuario, _) = await service.CriarAsync(new NovoUsuario
            {
                Nome = "Admin", Email = "admin@fundacao.org", Senha = "esqueci123", Perfil = Perfis.Administrador
            });
            usuario!.Ativo = false;
            await context.SaveChangesAsync();

            var erro = await service.RedefinirSenhaPorEmailAsync("ADMIN@fundacao.org", "senhanova123");

            Assert.Null(erro);
            Assert.NotNull(await service.AutenticarAsync("admin@fundacao.org", "senhanova123"));
        }
    }
}