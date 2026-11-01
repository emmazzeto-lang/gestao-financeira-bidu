using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanosTrabalhoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlanosTrabalhoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PlanosTrabalho?parceriaId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlanoTrabalho>>> GetPlanos(
            [FromQuery] int? parceriaId)
        {
            var consulta = _context.PlanosTrabalho.AsNoTracking();

            if (parceriaId.HasValue)
                consulta = consulta.Where(p => p.ParceriaId == parceriaId.Value);

            return await consulta.ToListAsync();
        }

        // GET: api/PlanosTrabalho/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlanoTrabalho>> GetPlano(int id)
        {
            var plano = await _context.PlanosTrabalho.FindAsync(id);

            if (plano == null)
                return NotFound(new { mensagem = "Plano de trabalho não encontrado." });

            return Ok(plano);
        }

        // POST: api/PlanosTrabalho
        [HttpPost]
        public async Task<ActionResult<PlanoTrabalho>> PostPlano(
            [FromBody] PlanoTrabalho plano)
        {
            if (plano.ValorAprovado <= 0)
                return BadRequest(new
                {
                    mensagem = "O valor aprovado deve ser maior que zero."
                });

            var parceria = await _context.Parcerias.FindAsync(plano.ParceriaId);

            if (parceria == null)
                return BadRequest(new
                {
                    mensagem = "A parceria informada não existe."
                });

            if (plano.ValorAprovado > parceria.ValorTotal)
                return BadRequest(new
                {
                    mensagem = "O valor do plano não pode passar o valor total da parceria."
                });

            // Ninguém cria um plano já validado pelo cadastro
            plano.Validado = false;
            plano.DataValidacao = null;
            plano.Parceria = null;
            plano.Metas = new();

            _context.PlanosTrabalho.Add(plano);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlano), new { id = plano.Id }, plano);
        }

        // PATCH: api/PlanosTrabalho/5/validar
        [HttpPatch("{id}/validar")]
        public async Task<IActionResult> ValidarPlano(int id)
        {
            var plano = await _context.PlanosTrabalho.FindAsync(id);

            if (plano == null)
                return NotFound(new { mensagem = "Plano de trabalho não encontrado." });

            if (plano.Validado)
                return Conflict(new { mensagem = "Este plano já foi validado." });

            var temMetas = await _context.Metas.AnyAsync(m => m.PlanoTrabalhoId == id);

            if (!temMetas)
                return BadRequest(new { mensagem = "Cadastre ao menos uma meta antes de validar o plano." });

            plano.Validado = true;
            plano.DataValidacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}