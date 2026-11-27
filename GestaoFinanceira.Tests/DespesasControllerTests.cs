using System.Security.Claims;
using GestaoFinanceira.Api.Controllers;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Tests
{
    public class DespesasControllerTests
    {
        // Controller com um usuário logado de Id 7, como se viesse do cookie
        private static DespesasController Controller(GestaoFinanceira.Api.Data.AppDbContext context)
        {
            var usuario = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "Teste"));

            return new DespesasController(context, new DespesaService(context))
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } }
            };
        }

        [Fact]
        public async Task CoordenacaoAutorizaOGastoAcimaDoPrevisto()
        {
            using var context = BancoDeTeste.Criar();
            var despesa = BancoDeTeste.NovaDespesa(valor: 1200);
            despesa.JustificativaExcesso = "Reajuste do fornecedor";
            context.Despesas.Add(despesa);
            await context.SaveChangesAsync();

            var resposta = await Controller(context).AutorizarExcesso(despesa.Id);

            Assert.IsType<NoContentResult>(resposta);
            Assert.Equal(7, despesa.AutorizadoPorId);
            Assert.NotNull(despesa.AutorizadoEm);
        }

        [Fact]
        public async Task DespesaDentroDoSaldoNaoTemOQueAutorizar()
        {
            using var context = BancoDeTeste.Criar();
            var despesa = BancoDeTeste.NovaDespesa(valor: 100);
            context.Despesas.Add(despesa);
            await context.SaveChangesAsync();

            var resposta = await Controller(context).AutorizarExcesso(despesa.Id);

            Assert.IsType<BadRequestObjectResult>(resposta);
            Assert.Null(despesa.AutorizadoPorId);
        }
    }
}
