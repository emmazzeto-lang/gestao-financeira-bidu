using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MetasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Metas/execucao?planoTrabalhoId=1
        [HttpGet("execucao")]
        public async Task<IActionResult> GetExecucao([FromQuery] int planoTrabalhoId)
        {
            var metas = await _context.Metas
                .AsNoTracking()
                .Where(m => m.PlanoTrabalhoId == planoTrabalhoId)
                .OrderBy(m => m.Descricao)
                .Select(m => new
                {
                    m.Id,
                    m.Descricao,
                    m.ValorPrevisto,
                    m.FonteRecursoId,
                    FonteRecurso = m.FonteRecurso!.Nome,
                    Executado = _context.Despesas
                        .Where(d => d.MetaId == m.Id)
                        .Sum(d => d.Valor)
                })
                .ToListAsync();

            return Ok(metas);
        }


        // GET: api/Metas?planoTrabalhoId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Meta>>> GetMetas(
            [FromQuery] int? planoTrabalhoId)
        {
            var consulta = _context.Metas
                .AsNoTracking()
                .Include(m => m.FonteRecurso)
                .AsQueryable();

            if (planoTrabalhoId.HasValue)
                consulta = consulta.Where(m => m.PlanoTrabalhoId == planoTrabalhoId.Value);

            return await consulta.ToListAsync();
        }

        // GET: api/Metas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Meta>> GetMeta(int id)
        {
            var meta = await _context.Metas
                .AsNoTracking()
                .Include(m => m.FonteRecurso)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meta == null)
                return NotFound(new { mensagem = "Meta não encontrada." });

            return Ok(meta);
        }

        // POST: api/Metas
        [HttpPost]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<ActionResult<Meta>> PostMeta([FromBody] Meta meta)
        {
            if (meta.ValorPrevisto <= 0)
                return BadRequest(new { mensagem = "O valor previsto deve ser maior que zero." });

            var plano = await _context.PlanosTrabalho
                .Include(p => p.Metas)
                .FirstOrDefaultAsync(p => p.Id == meta.PlanoTrabalhoId);

            if (plano == null)
                return BadRequest(new { mensagem = "O plano de trabalho informado não existe." });

            // Depois de validado, o plano fica "congelado"
            if (plano.Validado)
                return BadRequest(new { mensagem = "Este plano já foi validado e não pode receber novas metas." });

            var fonteExiste = await _context.FontesRecurso
                .AnyAsync(f => f.Id == meta.FonteRecursoId && f.Ativo);

            if (!fonteExiste)
                return BadRequest(new { mensagem = "A fonte de recurso informada não existe." });

            // A soma das metas não pode passar o valor aprovado do plano
            var jaPrevisto = plano.Metas.Sum(m => m.ValorPrevisto);
            var disponivel = plano.ValorAprovado - jaPrevisto;

            if (meta.ValorPrevisto > disponivel)
                return BadRequest(new
                {
                    mensagem = $"O valor passa o aprovado no plano. Ainda restam R$ {disponivel:N2} para distribuir."
                });

            // Evita inserir objetos relacionados enviados no JSON
            meta.PlanoTrabalho = null;
            meta.FonteRecurso = null;

            _context.Metas.Add(meta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeta), new { id = meta.Id }, meta);
        }
    }
}