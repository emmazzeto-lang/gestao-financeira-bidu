using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Tests
{
    public class UsuarioServiceTests
    {
        private static NovoUsuario Novo(string email = "ana@fundacao.org", string senha = "senhaforte1", string perfil = Perfis.Tesouraria)
        {
            return new NovoUsuario { Nome = "Ana Lima", Email = email, Senha = senha, Perfil = perfil };
        }

        [Fact]
        public async Task SenhaEhGuardadaComHashEOLoginFunciona()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);

            var (usuario, erro) = await service.CriarAsync(Novo());
            var logado = await service.AutenticarAsync("ana@fundacao.org", "senhaforte1");

            Assert.Null(erro);
            Assert.NotEqual("senhaforte1", usuario!.SenhaHash);
            Assert.Equal(usuario.Id, logado?.Id);
        }

        [Fact]
        public async Task EmailComMaiusculasEEspacosEntraIgual()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            await service.CriarAsync(Novo());

            var logado = await service.AutenticarAsync("  ANA@Fundacao.org ", "senhaforte1");

            Assert.NotNull(logado);
        }

        [Fact]
        public async Task SenhaErradaNaoEntra()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            await service.CriarAsync(Novo());

            Assert.Null(await service.AutenticarAsync("ana@fundacao.org", "senhaerrada"));
        }

        [Fact]
        public async Task UsuarioInativoNaoEntra()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            var (usuario, _) = await service.CriarAsync(Novo());
            usuario!.Ativo = false;
            await context.SaveChangesAsync();

            Assert.Null(await service.AutenticarAsync("ana@fundacao.org", "senhaforte1"));
        }

        [Fact]
        public async Task EmailRepetidoEhRecusado()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);
            await service.CriarAsync(Novo());

            var (_, erro) = await service.CriarAsync(Novo(email: "ANA@fundacao.org"));

            Assert.Equal("Já existe um usuário com este e-mail.", erro);
        }

        [Fact]
        public async Task SenhaCurtaEhRecusada()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);

            var (_, erro) = await service.CriarAsync(Novo(senha: "1234567"));

            Assert.Equal("A senha precisa ter pelo menos 8 caracteres.", erro);
        }

        [Fact]
        public async Task PerfilQueNaoExisteEhRecusado()
        {
            using var context = BancoDeTeste.Criar();
            var service = new UsuarioService(context);

            var (_, erro) = await service.CriarAsync(Novo(perfil: "Dono"));

            Assert.Equal("Perfil inválido.", erro);
        }
    }
}