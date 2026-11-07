using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConciliacaoController : ControllerBase
    {
        private readonly ConciliacaoService _conciliacaoService;

        public ConciliacaoController(ConciliacaoService conciliacaoService)
        {
            _conciliacaoService = conciliacaoService;
        }

        // GET: api/Conciliacao?contaBancariaId=1&ano=2026&mes=9
        [HttpGet]
        public async Task<ActionResult<Batimento>> GetBatimento(
            [FromQuery] int contaBancariaId, [FromQuery] int ano, [FromQuery] int mes)
        {
            if (mes < 1 || mes > 12 || ano < 2000)
                return BadRequest(new { mensagem = "Informe um mês e um ano válidos." });

            return Ok(await _conciliacaoService.MontarBatimentoAsync(contaBancariaId, ano, mes));
        }

        // POST: api/Conciliacao/importar (multipart: contaBancariaId + arquivo)
        [HttpPost("importar")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<ActionResult<ResultadoImportacao>> PostImportar([FromForm] ImportacaoExtrato importacao)
        {
            var (resultado, erro) = await _conciliacaoService.ImportarAsync(importacao);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return Ok(resultado);
        }

        // POST: api/Conciliacao/7  { "tipo": "despesa", "lancamentoId": 2 }
        [HttpPost("{movimentoId}")]
        public async Task<IActionResult> PostConciliar(int movimentoId, [FromBody] PedidoConciliacao pedido)
        {
            var erro = await _conciliacaoService.ConciliarAsync(movimentoId, pedido);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return NoContent();
        }

        // DELETE: api/Conciliacao/7
        [HttpDelete("{movimentoId}")]
        public async Task<IActionResult> DeleteConciliacao(int movimentoId)
        {
            var erro = await _conciliacaoService.DesfazerAsync(movimentoId);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return NoContent();
        }
    }
}