
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DespesasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DespesasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Despesas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Despesa>>> GetDespesas()
        {
            return await _context.Despesas
                .AsNoTracking()
                .Include(d => d.ContaBancaria)
                .Include(d => d.PlanoTrabalho)
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
            if (despesa.Valor <= 0)
                return BadRequest(new
                {
                    mensagem = "O valor da despesa deve ser maior que zero."
                });

            var contaExiste = await _context.ContasBancarias
                .AnyAsync(c => c.Id == despesa.ContaBancariaId);

            if (!contaExiste)
                return BadRequest(new
                {
                    mensagem = "A conta bancária informada não existe."
                });

            var planoExiste = await _context.PlanosTrabalho
                .AnyAsync(p => p.Id == despesa.PlanoTrabalhoId);

            if (!planoExiste)
                return BadRequest(new
                {
                    mensagem = "O plano de trabalho informado não existe."
                });

            // Evita inserir objetos relacionados enviados no JSON.
            despesa.ContaBancaria = null;
            despesa.PlanoTrabalho = null;

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

            if (despesa.Valor <= 0)
                return BadRequest(new
                {
                    mensagem = "O valor da despesa deve ser maior que zero."
                });

            var existente = await _context.Despesas.FindAsync(id);

            if (existente == null)
                return NotFound(new { mensagem = "Despesa não encontrada." });

            var contaExiste = await _context.ContasBancarias
                .AnyAsync(c => c.Id == despesa.ContaBancariaId);

            if (!contaExiste)
                return BadRequest(new
                {
                    mensagem = "A conta bancária informada não existe."
                });

            var planoExiste = await _context.PlanosTrabalho
                .AnyAsync(p => p.Id == despesa.PlanoTrabalhoId);

            if (!planoExiste)
                return BadRequest(new
                {
                    mensagem = "O plano de trabalho informado não existe."
                });

            existente.Descricao = despesa.Descricao;
            existente.Valor = despesa.Valor;
            existente.DataPagamento = despesa.DataPagamento;
            existente.ContaBancariaId = despesa.ContaBancariaId;
            existente.PlanoTrabalhoId = despesa.PlanoTrabalhoId;

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
