using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class ReceitaService
    {
        private readonly AppDbContext _context;

        public ReceitaService(AppDbContext context)
        {
            _context = context;
        }

        // Devolve a mensagem de erro, ou null se a receita puder ser gravada.
        public async Task<string?> ValidarAsync(Receita receita)
        {
            if (receita.Valor <= 0)
                return "O valor da receita deve ser maior que zero.";

            if (receita.DataRecebimento == default)
                return "Informe a data de recebimento.";

            var contaExiste = await _context.ContasBancarias
                .AnyAsync(c => c.Id == receita.ContaBancariaId);

            if (!contaExiste)
                return "A conta bancária informada não existe.";

            var plano = await _context.PlanosTrabalho
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == receita.PlanoTrabalhoId);

            if (plano == null)
                return "O plano de trabalho informado não existe.";

            // A regra principal: sem plano validado, não entra dinheiro
            if (!plano.Validado)
                return "A parceria só recebe aportes depois que o plano de trabalho for validado.";

            if (receita.FonteRecursoId == null)
                return "Informe a fonte de recurso da receita.";

            var fonteExiste = await _context.FontesRecurso
                .AnyAsync(f => f.Id == receita.FonteRecursoId && f.Ativo);

            if (!fonteExiste)
                return "A fonte de recurso informada não existe.";

            return null;
        }
    }
}