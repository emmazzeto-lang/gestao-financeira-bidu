using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class DespesaService
    {
        private readonly AppDbContext _context;

        public DespesaService(AppDbContext context)
        {
            _context = context;
        }

        // Devolve a mensagem de erro, ou null se a despesa puder ser gravada.
        // Na edição, idEmEdicao evita contar a própria despesa no saldo da meta.
        public async Task<string?> ValidarAsync(Despesa despesa, int idEmEdicao = 0)
        {
            if (despesa.Valor <= 0)
                return "O valor da despesa deve ser maior que zero.";

            var contaExiste = await _context.ContasBancarias
                .AnyAsync(c => c.Id == despesa.ContaBancariaId);

            if (!contaExiste)
                return "A conta bancária informada não existe.";

            var plano = await _context.PlanosTrabalho
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == despesa.PlanoTrabalhoId);

            if (plano == null)
                return "O plano de trabalho informado não existe.";

            if (!plano.Validado)
                return "O plano de trabalho ainda não foi validado.";

            if (despesa.MetaId == null)
                return "Informe a meta da despesa.";

            var meta = await _context.Metas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == despesa.MetaId);

            if (meta == null)
                return "A meta informada não existe.";

            if (meta.PlanoTrabalhoId != despesa.PlanoTrabalhoId)
                return "A meta informada não pertence a este plano de trabalho.";

            if (despesa.FornecedorId != null)
            {
                var fornecedorExiste = await _context.Fornecedores
                    .AnyAsync(f => f.Id == despesa.FornecedorId && f.Ativo);

                if (!fornecedorExiste)
                    return "O fornecedor informado não existe.";
            }

            var jaExecutado = await _context.Despesas
                .Where(d => d.MetaId == meta.Id && d.Id != idEmEdicao)
                .SumAsync(d => d.Valor);

            var disponivel = meta.ValorPrevisto - jaExecutado;

            if (despesa.Valor > disponivel)
                return $"O valor passa o saldo da meta. Disponível: R$ {disponivel:N2}.";

            return null;
        }
    }
}