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
    public class DocumentosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoService _documentoService;

        public DocumentosController(AppDbContext context, DocumentoService documentoService)
        {
            _context = context;
            _documentoService = documentoService;
        }

        // GET: api/Documentos?despesaId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos(
            [FromQuery] int? despesaId)
        {
            var consulta = _context.Documentos.AsNoTracking();

            if (despesaId.HasValue)
                consulta = consulta.Where(d => d.DespesaId == despesaId.Value);

            return await consulta
                .OrderByDescending(d => d.DataEnvio)
                .ToListAsync();
        }

        // GET: api/Documentos/despesas-sem-comprovante
        [HttpGet("despesas-sem-comprovante")]
        public async Task<ActionResult<IEnumerable<Despesa>>> GetDespesasSemComprovante()
        {
            return await _context.Despesas
                .AsNoTracking()
                .Where(d => !_context.Documentos.Any(doc => doc.DespesaId == d.Id))
                .OrderBy(d => d.DataPagamento)
                .ToListAsync();
        }

        // GET: api/Documentos/repetido?tipo=NF-e&numero=004512&fornecedorId=1
        [HttpGet("repetido")]
        public async Task<IActionResult> GetRepetido(
            [FromQuery] string tipo, [FromQuery] string numero, [FromQuery] int? fornecedorId)
        {
            var repetido = await _documentoService.BuscarRepetidoAsync(tipo, numero, fornecedorId);

            // despesaId vem nulo quando o documento ainda não foi usado
            return Ok(new { despesaId = repetido?.DespesaId });
        }

        // GET: api/Documentos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Documento>> GetDocumento(int id)
        {
            var documento = await _context.Documentos.FindAsync(id);

            if (documento == null)
                return NotFound(new { mensagem = "Documento não encontrado." });

            return Ok(documento);
        }

        // GET: api/Documentos/5/arquivo
        [HttpGet("{id}/arquivo")]
        public async Task<IActionResult> BaixarArquivo(int id)
        {
            var documento = await _context.Documentos.FindAsync(id);

            if (documento == null)
                return NotFound(new { mensagem = "Documento não encontrado." });

            var caminho = _documentoService.CaminhoDoArquivo(documento);

            if (!System.IO.File.Exists(caminho))
                return NotFound(new { mensagem = "O arquivo não foi encontrado no armazenamento." });

            return PhysicalFile(caminho, documento.TipoConteudo, documento.NomeOriginal);
        }

        // POST: api/Documentos
        [HttpPost]
        [Authorize(Roles = Perfis.PodeLancar)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<ActionResult<Documento>> PostDocumento([FromForm] EnvioDocumento envio)
        {
            var (documento, erro) = await _documentoService.EnviarAsync(envio);

            if (erro != null)
                return BadRequest(new { mensagem = erro });

            return CreatedAtAction(nameof(GetDocumento), new { id = documento!.Id }, documento);
        }

        // DELETE: api/Documentos/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<IActionResult> DeleteDocumento(int id)
        {
            var documento = await _context.Documentos.FindAsync(id);

            if (documento == null)
                return NotFound(new { mensagem = "Documento não encontrado." });

            var caminho = _documentoService.CaminhoDoArquivo(documento);

            _context.Documentos.Remove(documento);
            await _context.SaveChangesAsync();

            if (System.IO.File.Exists(caminho))
                System.IO.File.Delete(caminho);

            return NoContent();
        }
    }
}