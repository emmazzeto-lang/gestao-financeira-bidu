using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PainelController : ControllerBase
    {
        private readonly PainelService _painelService;

        public PainelController(PainelService painelService)
        {
            _painelService = painelService;
        }

        // GET: api/Painel/razao?contaBancariaId=1&ano=2026&mes=9 (ano e mês são opcionais)
        [HttpGet("razao")]
        public async Task<ActionResult<Razao>> GetRazao(
            [FromQuery] int contaBancariaId, [FromQuery] int? ano, [FromQuery] int? mes)
        {
            if (mes is < 1 or > 12)
                return BadRequest(new { mensagem = "Informe um mês válido." });

            var razao = await _painelService.RazaoAsync(contaBancariaId, ano, mes);

            if (razao == null)
                return NotFound(new { mensagem = "Conta bancária não encontrada." });

            return Ok(razao);
        }

        // GET: api/Painel/contadores
        [HttpGet("contadores")]
        public async Task<ActionResult<Contadores>> GetContadores()
        {
            return Ok(await _painelService.ContadoresAsync());
        }
    }
}