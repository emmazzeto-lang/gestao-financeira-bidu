using GestaoFinanceira.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Tests
{
    // Aqui o teste chama o controller direto, sem servidor: é só uma classe C#
    public class MetasControllerTests
    {
        [Fact]
        public async Task MetaDePlanoAbertoPodeSerRemovida()
        {
            using var context = BancoDeTeste.Criar();
            var controller = new MetasController(context);

            // A meta 2 é do plano 2, que ainda não foi validado
            var resposta = await controller.DeleteMeta(2);

            Assert.IsType<NoContentResult>(resposta);
            Assert.Null(await context.Metas.FindAsync(2));
        }

        [Fact]
        public async Task MetaDePlanoValidadoNaoSai()
        {
            using var context = BancoDeTeste.Criar();
            var controller = new MetasController(context);

            // A meta 1 é do plano 1, que já está validado
            var resposta = await controller.DeleteMeta(1);

            Assert.IsType<BadRequestObjectResult>(resposta);
            Assert.NotNull(await context.Metas.FindAsync(1));
        }

        [Fact]
        public async Task MetaComDespesaNaoSai()
        {
            using var context = BancoDeTeste.Criar();
            context.Despesas.Add(BancoDeTeste.NovaDespesa(planoId: 2, metaId: 2));
            await context.SaveChangesAsync();
            var controller = new MetasController(context);

            var resposta = await controller.DeleteMeta(2);

            Assert.IsType<ConflictObjectResult>(resposta);
        }
    }
}