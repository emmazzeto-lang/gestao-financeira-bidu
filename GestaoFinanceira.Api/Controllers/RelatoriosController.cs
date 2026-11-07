using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelatoriosController : ControllerBase
    {
        private readonly RelatorioService _relatorioService;

        public RelatoriosController(RelatorioService relatorioService)
        {
            _relatorioService = relatorioService;
        }

        // GET: api/Relatorios/execucao?parceriaId=1
        [HttpGet("execucao")]
        public async Task<ActionResult<RelatorioExecucao>> GetExecucao([FromQuery] int parceriaId)
        {
            var relatorio = await _relatorioService.ExecucaoAsync(parceriaId);

            if (relatorio == null)
                return NotFound(new { mensagem = "Parceria não encontrada." });

            return Ok(relatorio);
        }

        // GET: api/Relatorios/execucao/csv?parceriaId=1
        [HttpGet("execucao/csv")]
        public async Task<IActionResult> GetExecucaoCsv([FromQuery] int parceriaId)
        {
            var relatorio = await _relatorioService.ExecucaoAsync(parceriaId);

            if (relatorio == null)
                return NotFound(new { mensagem = "Parceria não encontrada." });

            // "TF 012/2025" vira "TF-012-2025" para poder ser nome de arquivo
            var nome = $"execucao-{relatorio.TermoFomento.Replace(' ', '-').Replace('/', '-')}.csv";

            return File(RelatorioService.ExecucaoEmCsv(relatorio), "text/csv", nome);
        }

        // GET: api/Relatorios/rastreio/2
        [HttpGet("rastreio/{despesaId}")]
        public async Task<ActionResult<List<EtapaRastreio>>> GetRastreio(int despesaId)
        {
            var etapas = await _relatorioService.RastrearAsync(despesaId);

            if (etapas == null)
                return NotFound(new { mensagem = $"Despesa Nº {despesaId} não encontrada." });

            return Ok(etapas);
        }
    }
}