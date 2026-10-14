using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;

namespace GestaoFinanceira.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParceriasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParceriasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Parcerias (Lista todas as parcerias)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Parceria>>> GetParcerias()
        {
            return await _context.Parcerias.ToListAsync();
        }

        // POST: api/Parcerias (Cria uma nova parceria)
        [HttpPost]
        public async Task<ActionResult<Parceria>> PostParceria(Parceria parceria)
        {
            _context.Parcerias.Add(parceria);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParcerias), new { id = parceria.Id }, parceria);
        }
    }
}