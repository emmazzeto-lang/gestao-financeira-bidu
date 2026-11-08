using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FontesRecursoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FontesRecursoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/FontesRecurso
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FonteRecurso>>> GetFontes()
        {
            return await _context.FontesRecurso
                .AsNoTracking()
                .Where(f => f.Ativo)
                .OrderBy(f => f.Nome)
                .ToListAsync();
        }

        // POST: api/FontesRecurso
        [HttpPost]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<ActionResult<FonteRecurso>> PostFonte(
            [FromBody] FonteRecurso fonte)
        {
            fonte.Nome = fonte.Nome.Trim();

            if (string.IsNullOrWhiteSpace(fonte.Nome))
                return BadRequest(new { mensagem = "Informe o nome da fonte de recurso." });

            var jaExiste = await _context.FontesRecurso
                .AnyAsync(f => f.Nome.ToLower() == fonte.Nome.ToLower());

            if (jaExiste)
                return Conflict(new { mensagem = "Já existe uma fonte de recurso com esse nome." });

            _context.FontesRecurso.Add(fonte);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFontes), new { id = fonte.Id }, fonte);
        }
    }
}