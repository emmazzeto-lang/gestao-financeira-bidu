using GestaoFinanceira.Api.Controllers;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Tests
{
    public class PlanosTrabalhoControllerTests
    {
        [Fact]
        public async Task PlanoComValorSobrandoNaoValida()
        {
            using var context = BancoDeTeste.Criar();
            var controller = new PlanosTrabalhoController(context);

            // Plano 2: R$ 5.000 aprovados, só R$ 1.000 na meta 2
            var resposta = await controller.ValidarPlano(2);

            Assert.IsType<BadRequestObjectResult>(resposta);
            Assert.False((await context.PlanosTrabalho.FindAsync(2))!.Validado);
        }

        [Fact]
        public async Task PlanoTodoDistribuidoValida()
        {
            using var context = BancoDeTeste.Criar();
            context.Metas.Add(new Meta { Descricao = "Resto do plano", ValorPrevisto = 4000, PlanoTrabalhoId = 2, FonteRecursoId = 1 });
            await context.SaveChangesAsync();
            var controller = new PlanosTrabalhoController(context);

            var resposta = await controller.ValidarPlano(2);

            Assert.IsType<NoContentResult>(resposta);
            Assert.True((await context.PlanosTrabalho.FindAsync(2))!.Validado);
        }
    }
}