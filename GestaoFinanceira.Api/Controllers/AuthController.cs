using System.Security.Claims;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;

        public AuthController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // POST: api/Auth/entrar
        [HttpPost("entrar")]
        [AllowAnonymous]
        public async Task<ActionResult<UsuarioResumo>> Entrar([FromBody] Login login)
        {
            var usuario = await _usuarioService.AutenticarAsync(login.Email, login.Senha);

            // A mesma mensagem para e-mail e senha: não revela quais e-mails existem
            if (usuario == null)
                return Unauthorized(new { mensagem = "E-mail ou senha incorretos." });

            // Claims são as informações que ficam guardadas no cookie de sessão
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Nome),
                new(ClaimTypes.Email, usuario.Email),
                new(ClaimTypes.Role, usuario.Perfil)
            };

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidade),
                new AuthenticationProperties { IsPersistent = login.ManterConectado });

            return Ok(UsuarioService.Resumo(usuario));
        }

        // POST: api/Auth/sair
        [HttpPost("sair")]
        [AllowAnonymous]
        public async Task<IActionResult> Sair()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        // GET: api/Auth/eu (quem está logado agora)
        [HttpGet("eu")]
        public ActionResult<UsuarioResumo> Eu()
        {
            return Ok(new UsuarioResumo(
                int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                User.FindFirstValue(ClaimTypes.Name)!,
                User.FindFirstValue(ClaimTypes.Email)!,
                User.FindFirstValue(ClaimTypes.Role)!,
                true));
        }

        // POST: api/Auth/primeiro-acesso
        // Só funciona com o banco sem nenhum usuário: cria o primeiro administrador
        [HttpPost("primeiro-acesso")]
        [AllowAnonymous]
        public async Task<ActionResult<UsuarioResumo>> PrimeiroAcesso([FromBody] NovoUsuario novo)
        {
            if (await _usuarioService.ExisteAlgumAsync())
                return Conflict(new { mensagem = "O primeiro acesso já foi feito. Peça um usuário ao administrador." });

            novo.Perfil = Perfis.Administrador;
            var (usuario, erro) = await _usuarioService.CriarAsync(novo);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return Ok(UsuarioService.Resumo(usuario!));
        }
    }
}