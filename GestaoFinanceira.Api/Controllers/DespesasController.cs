
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Authorization;

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
        [Authorize(Roles = Perfis.PodeLancar)]
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
            despesa.CriadoPor = null;

            // A autorização só vem da coordenação, pelo PATCH .../autorizar
            despesa.AutorizadoPor = null;
            despesa.AutorizadoPorId = null;
            despesa.AutorizadoEm = null;

            // Quem está logado fica registrado como autor do lançamento
            despesa.CriadoPorId = User.IdDoUsuario();
            despesa.CriadoEm = DateTime.UtcNow;

            _context.Despesas.Add(despesa);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDespesa),
                new { id = despesa.Id },
                despesa);
        }

        // PUT: api/Despesas/5
        [HttpPut("{id}")]
        [Authorize(Roles = Perfis.PodeLancar)]
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

            // Mudou o que foi autorizado: a coordenação precisa autorizar de novo
            if (existente.Valor != despesa.Valor || existente.MetaId != despesa.MetaId
                || existente.JustificativaExcesso != despesa.JustificativaExcesso)
            {
                existente.AutorizadoPorId = null;
                existente.AutorizadoEm = null;
            }

            existente.JustificativaExcesso = despesa.JustificativaExcesso;
            existente.Descricao = despesa.Descricao;
            existente.Valor = despesa.Valor;
            existente.DataPagamento = despesa.DataPagamento;
            existente.Competencia = despesa.Competencia;
            existente.FormaPagamento = despesa.FormaPagamento;
            existente.ContaBancariaId = despesa.ContaBancariaId;
            existente.PlanoTrabalhoId = despesa.PlanoTrabalhoId;
            existente.MetaId = despesa.MetaId;
            existente.FornecedorId = despesa.FornecedorId;
            existente.ComprovadaPeloExtrato = despesa.ComprovadaPeloExtrato;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Despesas/acima-do-previsto?planoTrabalhoId=1
        // Despesas que passaram o saldo da meta, autorizadas ou não
        [HttpGet("acima-do-previsto")]
        public async Task<ActionResult<IEnumerable<GastoAcimaDoPrevisto>>> GetAcimaDoPrevisto(
            [FromQuery] int? planoTrabalhoId)
        {
            var consulta = _context.Despesas
                .AsNoTracking()
                .Where(d => d.JustificativaExcesso != null);

            if (planoTrabalhoId.HasValue)
                consulta = consulta.Where(d => d.PlanoTrabalhoId == planoTrabalhoId.Value);

            return await consulta
                .OrderByDescending(d => d.DataPagamento)
                .Select(d => new GastoAcimaDoPrevisto(
                    d.Id,
                    d.Descricao,
                    d.Valor,
                    d.DataPagamento,
                    d.Meta != null ? d.Meta.Descricao : "Sem meta",
                    d.JustificativaExcesso!,
                    d.CriadoPor != null ? d.CriadoPor.Nome : null,
                    d.AutorizadoPor != null ? d.AutorizadoPor.Nome : null,
                    d.AutorizadoEm))
                .ToListAsync();
        }

        // PATCH: api/Despesas/5/autorizar
        // A coordenação autoriza o gasto acima do previsto na meta
        [HttpPatch("{id}/autorizar")]
        [Authorize(Roles = Perfis.PodeValidar)]
        public async Task<IActionResult> AutorizarExcesso(int id)
        {
            var despesa = await _context.Despesas.FindAsync(id);

            if (despesa == null)
                return NotFound(new { mensagem = "Despesa não encontrada." });

            if (despesa.JustificativaExcesso == null)
                return BadRequest(new { mensagem = "Esta despesa cabe no saldo da meta e não precisa de autorização." });

            if (despesa.AutorizadoPorId != null)
                return Conflict(new { mensagem = "Esta despesa já foi autorizada." });

            despesa.AutorizadoPorId = User.IdDoUsuario();
            despesa.AutorizadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Despesas/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<IActionResult> DeleteDespesa(int id)
        {
            var despesa = await _context.Despesas.FindAsync(id);

            if (despesa == null)
                return NotFound(new { mensagem = "Despesa não encontrada." });

            var temComprovante = await _context.Documentos
                .AnyAsync(d => d.DespesaId == id);

            if (temComprovante)
                return Conflict(new { mensagem = "Remova os comprovantes antes de excluir a despesa." });

            var conciliada = await _context.MovimentosExtrato
                .AnyAsync(m => m.DespesaId == id);

            if (conciliada)
                return Conflict(new { mensagem = "Desfaça a conciliação antes de excluir a despesa." });

            _context.Despesas.Remove(despesa);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
