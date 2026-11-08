using System.Security.Claims;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Controllers
{
    // No controller inteiro: só o administrador gerencia usuários
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Perfis.Administrador)]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UsuarioService _usuarioService;

        public UsuariosController(AppDbContext context, UsuarioService usuarioService)
        {
            _context = context;
            _usuarioService = usuarioService;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResumo>>> GetUsuarios()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nome)
                .Select(u => new UsuarioResumo(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo))
                .ToListAsync();
        }

        // POST: api/Usuarios
        [HttpPost]
        public async Task<ActionResult<UsuarioResumo>> PostUsuario([FromBody] NovoUsuario novo)
        {
            var (usuario, erro) = await _usuarioService.CriarAsync(novo);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return Ok(UsuarioService.Resumo(usuario!));
        }

        // PUT: api/Usuarios/5  { "nome": "...", "perfil": "Tesouraria", "ativo": true }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, [FromBody] AlteracaoUsuario alteracao)
        {
            var idLogado = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var erro = await _usuarioService.AlterarAsync(id, alteracao, idLogado);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return NoContent();
        }

        // PATCH: api/Usuarios/5/senha  { "senha": "..." }
        [HttpPatch("{id}/senha")]
        public async Task<IActionResult> PatchSenha(int id, [FromBody] NovaSenha novaSenha)
        {
            var erro = await _usuarioService.RedefinirSenhaAsync(id, novaSenha.Senha);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return NoContent();
        }
    }
}