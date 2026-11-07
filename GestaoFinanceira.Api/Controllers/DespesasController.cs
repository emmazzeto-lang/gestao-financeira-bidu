
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DespesasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DespesaService _despesaService;

        public DespesasController(AppDbContext context, DespesaService despesaService)
        {
            _context = context;
            _despesaService = despesaService;
        }

        // GET: api/Despesas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Despesa>>> GetDespesas()
        {
            return await _context.Despesas
                .AsNoTracking()
                .Include(d => d.ContaBancaria)
                .Include(d => d.PlanoTrabalho)
                .Include(d => d.Meta)
                .Include(d => d.Fornecedor)
                .ToListAsync();
        }

        // GET: api/Despesas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Despesa>> GetDespesa(int id)
        {
            var despesa = await _context.Despesas
                .AsNoTracking()
                .Include(d => d.ContaBancaria)
                .Include(d => d.PlanoTrabalho)
                .Include(d => d.Meta)
                .Include(d => d.Fornecedor)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (despesa == null)
                return NotFound(new { mensagem = "Despesa não encontrada." });

            return Ok(despesa);
        }

        // POST: api/Despesas
        [HttpPost]
        public async Task<ActionResult<Despesa>> PostDespesa(
            [FromBody] Despesa despesa)
        {
            var erro = await _despesaService.ValidarAsync(despesa);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            // Evita inserir objetos relacionados enviados no JSON.
            despesa.ContaBancaria = null;
            despesa.PlanoTrabalho = null;
            despesa.Meta = null;
            despesa.Fornecedor = null;

            _context.Despesas.Add(despesa);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDespesa),
                new { id = despesa.Id },
                despesa);
        }

        // PUT: api/Despesas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDespesa(
            int id, [FromBody] Despesa despesa)
        {
            if (id != despesa.Id)
                return BadRequest(new
                {
                    mensagem = "O ID da rota é diferente do ID da despesa."
                });

            var existente = await _context.Despesas.FindAsync(id);

            if (existente == null)
                return NotFound(new { mensagem = "Despesa não encontrada." });

            var erro = await _despesaService.ValidarAsync(despesa, id);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            existente.Descricao = despesa.Descricao;
            existente.Valor = despesa.Valor;
            existente.DataPagamento = despesa.DataPagamento;
            existente.ContaBancariaId = despesa.ContaBancariaId;
            existente.PlanoTrabalhoId = despesa.PlanoTrabalhoId;
            existente.MetaId = despesa.MetaId;
            existente.FornecedorId = despesa.FornecedorId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Despesas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDespesa(int id)
        {
            var despesa = await _context.Despesas.FindAsync(id);

            if (despesa == null)
                return NotFound(new { mensagem = "Despesa não encontrada." });

            _context.Despesas.Remove(despesa);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
