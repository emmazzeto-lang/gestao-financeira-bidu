using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FornecedoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FornecedoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Fornecedores (só ativos) ou api/Fornecedores?todos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Fornecedor>>> GetFornecedores([FromQuery] bool todos = false)
        {
            return await _context.Fornecedores
                .AsNoTracking()
                .Where(f => todos || f.Ativo)
                .OrderBy(f => f.Nome)
                .ToListAsync();
        }

        // GET: api/Fornecedores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Fornecedor>> GetFornecedor(int id)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);

            if (fornecedor == null)
                return NotFound(new { mensagem = "Fornecedor não encontrado." });

            return Ok(fornecedor);
        }

        // POST: api/Fornecedores
        [HttpPost]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<ActionResult<Fornecedor>> PostFornecedor(
            [FromBody] Fornecedor fornecedor)
        {
            fornecedor.Nome = fornecedor.Nome.Trim();
            fornecedor.CpfCnpj = ValidadorCpfCnpj.Normalizar(fornecedor.CpfCnpj);

            if (fornecedor.Nome == "")
                return BadRequest(new { mensagem = "Informe o nome do fornecedor." });

            if (!ValidadorCpfCnpj.Valido(fornecedor.CpfCnpj))
                return BadRequest(new { mensagem = "CPF ou CNPJ inválido. Confira os números." });

            var jaExiste = await _context.Fornecedores
                .AnyAsync(f => f.CpfCnpj == fornecedor.CpfCnpj);

            if (jaExiste)
                return Conflict(new { mensagem = "Já existe um fornecedor com esse CPF/CNPJ." });

            _context.Fornecedores.Add(fornecedor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFornecedor), new { id = fornecedor.Id }, fornecedor);
        }

        // PUT: api/Fornecedores/5
        [HttpPut("{id}")]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<IActionResult> PutFornecedor(
            int id, [FromBody] Fornecedor fornecedor)
        {
            if (id != fornecedor.Id)
                return BadRequest(new { mensagem = "O ID da rota é diferente do ID do fornecedor." });

            var existente = await _context.Fornecedores.FindAsync(id);

            if (existente == null)
                return NotFound(new { mensagem = "Fornecedor não encontrado." });

            var cpfCnpj = ValidadorCpfCnpj.Normalizar(fornecedor.CpfCnpj);

            if (fornecedor.Nome.Trim() == "")
                return BadRequest(new { mensagem = "Informe o nome do fornecedor." });

            if (!ValidadorCpfCnpj.Valido(cpfCnpj))
                return BadRequest(new { mensagem = "CPF ou CNPJ inválido. Confira os números." });

            var jaExiste = await _context.Fornecedores
                .AnyAsync(f => f.CpfCnpj == cpfCnpj && f.Id != id);

            if (jaExiste)
                return Conflict(new { mensagem = "Já existe um fornecedor com esse CPF/CNPJ." });

            existente.Nome = fornecedor.Nome.Trim();
            existente.CpfCnpj = cpfCnpj;
            existente.Ativo = fornecedor.Ativo;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}