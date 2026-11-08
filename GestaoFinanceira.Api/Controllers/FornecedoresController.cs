using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/Fornecedores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Fornecedor>>> GetFornecedores()
        {
            return await _context.Fornecedores
                .AsNoTracking()
                .Where(f => f.Ativo)
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
            fornecedor.CpfCnpj = SomenteNumeros(fornecedor.CpfCnpj);

            if (fornecedor.CpfCnpj.Length != 11 && fornecedor.CpfCnpj.Length != 14)
                return BadRequest(new { mensagem = "Informe um CPF (11 dígitos) ou CNPJ (14 dígitos)." });

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

            var cpfCnpj = SomenteNumeros(fornecedor.CpfCnpj);

            if (cpfCnpj.Length != 11 && cpfCnpj.Length != 14)
                return BadRequest(new { mensagem = "Informe um CPF (11 dígitos) ou CNPJ (14 dígitos)." });

            var jaExiste = await _context.Fornecedores
                .AnyAsync(f => f.CpfCnpj == cpfCnpj && f.Id != id);

            if (jaExiste)
                return Conflict(new { mensagem = "Já existe um fornecedor com esse CPF/CNPJ." });

            existente.Nome = fornecedor.Nome;
            existente.CpfCnpj = cpfCnpj;
            existente.Ativo = fornecedor.Ativo;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // "75.315.333/0001-09" vira "75315333000109"
        private static string SomenteNumeros(string texto)
        {
            return new string(texto.Where(char.IsDigit).ToArray());
        }
    }
}