using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceitasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ReceitaService _receitaService;

        public ReceitasController(AppDbContext context, ReceitaService receitaService)
        {
            _context = context;
            _receitaService = receitaService;
        }

        // GET: api/Receitas?planoTrabalhoId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Receita>>> GetReceitas(
            [FromQuery] int? planoTrabalhoId)
        {
            var consulta = _context.Receitas
                .AsNoTracking()
                .Include(r => r.ContaBancaria)
                .Include(r => r.FonteRecurso)
                .AsQueryable();

            if (planoTrabalhoId.HasValue)
                consulta = consulta.Where(r => r.PlanoTrabalhoId == planoTrabalhoId.Value);

            return await consulta
                .OrderByDescending(r => r.DataRecebimento)
                .ToListAsync();
        }

        // GET: api/Receitas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Receita>> GetReceita(int id)
        {
            var receita = await _context.Receitas
                .AsNoTracking()
                .Include(r => r.ContaBancaria)
                .Include(r => r.FonteRecurso)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receita == null)
                return NotFound(new { mensagem = "Receita não encontrada." });

            return Ok(receita);
        }

        // POST: api/Receitas
        [HttpPost]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<ActionResult<Receita>> PostReceita([FromBody] Receita receita)
        {
            var erro = await _receitaService.ValidarAsync(receita);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            // Evita inserir objetos relacionados enviados no JSON.
            receita.ContaBancaria = null;
            receita.PlanoTrabalho = null;
            receita.CriadoPor = null;

            // Quem está logado fica registrado como autor do lançamento
            receita.CriadoPorId = User.IdDoUsuario();
            receita.CriadoEm = DateTime.UtcNow;

            _context.Receitas.Add(receita);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReceita), new { id = receita.Id }, receita);
        }

        // PUT: api/Receitas/5
        [HttpPut("{id}")]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<IActionResult> PutReceita(int id, [FromBody] Receita receita)
        {
            if (id != receita.Id)
                return BadRequest(new { mensagem = "O ID da rota é diferente do ID da receita." });

            var existente = await _context.Receitas.FindAsync(id);

            if (existente == null)
                return NotFound(new { mensagem = "Receita não encontrada." });

            var erro = await _receitaService.ValidarAsync(receita);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            existente.Descricao = receita.Descricao;
            existente.Valor = receita.Valor;
            existente.DataRecebimento = receita.DataRecebimento;
            existente.ContaBancariaId = receita.ContaBancariaId;
            existente.PlanoTrabalhoId = receita.PlanoTrabalhoId;
            existente.FonteRecursoId = receita.FonteRecursoId;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}