using System.Reflection;
using GestaoFinanceira.Api.Controllers;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace GestaoFinanceira.Tests
{
    public class PerfisTests
    {
        // Rede de segurança: se alguém criar uma rota que grava e esquecer o [Authorize(Roles = ...)],
        // este teste falha e mostra qual foi.
        [Fact]
        public void TodaRotaQueGravaExigePerfil()
        {
            var controllers = typeof(ParceriasController).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t != typeof(AuthController));

            var semPerfil = new List<string>();

            foreach (var controller in controllers)
            {
                var perfilNoController = controller.GetCustomAttribute<AuthorizeAttribute>()?.Roles;

                foreach (var acao in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var verbos = acao.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods);
                    if (!verbos.Any(v => v != "GET"))
                        continue;

                    var perfilNaAcao = acao.GetCustomAttribute<AuthorizeAttribute>()?.Roles;
                    if (string.IsNullOrEmpty(perfilNaAcao ?? perfilNoController))
                        semPerfil.Add($"{controller.Name}.{acao.Name}");
                }
            }

            Assert.Empty(semPerfil);
        }

        [Fact]
        public void SoDiretoriaEAdministradorValidamOPlano()
        {
            var validar = typeof(PlanosTrabalhoController).GetMethod(nameof(PlanosTrabalhoController.ValidarPlano))!;

            var perfis = validar.GetCustomAttribute<AuthorizeAttribute>()!.Roles!.Split(',');

            Assert.Contains(Perfis.Diretoria, perfis);
            Assert.DoesNotContain(Perfis.Tesouraria, perfis);
        }

        [Fact]
        public async Task AdministradorNaoTiraOProprioAcesso()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            var (admin, _) = await service.CriarAsync(new NovoUsuario
            {
                Nome = "Admin", Email = "admin@fundacao.org", Senha = "senhaforte1", Perfil = Perfis.Administrador
            });

            var erro = await service.AlterarAsync(admin!.Id,
                new AlteracaoUsuario { Nome = "Admin", Perfil = Perfis.Consulta, Ativo = true }, admin.Id);

            Assert.Equal("Você não pode desativar nem tirar o perfil de administrador de si mesmo.", erro);
        }

        [Fact]
        public async Task RedefinirSenhaTrocaASenhaDeEntrada()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            var (usuario, _) = await service.CriarAsync(new NovoUsuario
            {
                Nome = "Ana", Email = "ana@fundacao.org", Senha = "senhaantiga", Perfil = Perfis.Tesouraria
            });

            await service.RedefinirSenhaAsync(usuario!.Id, "senhanova123");

            Assert.Null(await service.AutenticarAsync("ana@fundacao.org", "senhaantiga"));
            Assert.NotNull(await service.AutenticarAsync("ana@fundacao.org", "senhanova123"));
        }
    }
}