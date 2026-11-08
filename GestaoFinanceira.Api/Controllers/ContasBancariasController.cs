
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContasBancariasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContasBancariasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ContasBancarias
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContaBancaria>>> GetContasBancarias()
        {
            return await _context.ContasBancarias
                .AsNoTracking()
                .ToListAsync();
        }

        // GET: api/ContasBancarias/1
        [HttpGet("{id}")]
        public async Task<ActionResult<ContaBancaria>> GetContaBancaria(int id)
        {
            var conta = await _context.ContasBancarias.FindAsync(id);

            if (conta == null)
                return NotFound(new { mensagem = "Conta bancária não encontrada." });

            return Ok(conta);
        }

        // POST: api/ContasBancarias
        [HttpPost]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<ActionResult<ContaBancaria>> PostContaBancaria(
            ContaBancaria conta)
        {
            if (string.IsNullOrWhiteSpace(conta.NomeBanco) ||
                string.IsNullOrWhiteSpace(conta.Agencia) ||
                string.IsNullOrWhiteSpace(conta.NumeroConta))
            {
                return BadRequest(new
                {
                    mensagem = "Banco, agência e número da conta são obrigatórios."
                });
            }

            if (conta.SaldoAtual < 0)
                return BadRequest(new
                {
                    mensagem = "O saldo inicial não pode ser negativo."
                });

            _context.ContasBancarias.Add(conta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetContaBancaria),
                new { id = conta.Id },
                conta);
        }

        // PUT: api/ContasBancarias/1
        [HttpPut("{id}")]
        [Authorize(Roles = Perfis.PodeLancar)]
        public async Task<IActionResult> PutContaBancaria(int id, ContaBancaria conta)
        {
            if (id != conta.Id)
                return BadRequest(new { mensagem = "O ID da rota é diferente do ID da conta." });

            var existente = await _context.ContasBancarias.FindAsync(id);

            if (existente == null)
                return NotFound(new { mensagem = "Conta bancária não encontrada." });

            if (string.IsNullOrWhiteSpace(conta.NomeBanco) ||
                string.IsNullOrWhiteSpace(conta.Agencia) ||
                string.IsNullOrWhiteSpace(conta.NumeroConta))
            {
                return BadRequest(new
                {
                    mensagem = "Banco, agência e número da conta são obrigatórios."
                });
            }

            // O saldo inicial fica como foi cadastrado: só muda banco, agência, número e situação
            existente.NomeBanco = conta.NomeBanco.Trim();
            existente.Agencia = conta.Agencia.Trim();
            existente.NumeroConta = conta.NumeroConta.Trim();
            existente.Ativo = conta.Ativo;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
