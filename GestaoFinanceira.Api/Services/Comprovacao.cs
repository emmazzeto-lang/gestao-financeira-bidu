using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;

namespace GestaoFinanceira.Api.Services
{
    // Uma despesa está comprovada quando tem documento ou, no caso de uma tarifa bancária,
    // quando já foi conciliada com a linha do extrato que mostra a cobrança
    public static class Comprovacao
    {
        public static IQueryable<Despesa> SemComprovante(this IQueryable<Despesa> despesas, AppDbContext context)
        {
            return despesas.Where(d =>
                !context.Documentos.Any(doc => doc.DespesaId == d.Id)
                && !(d.ComprovadaPeloExtrato && context.MovimentosExtrato.Any(m => m.DespesaId == d.Id)));
        }
    }
}
