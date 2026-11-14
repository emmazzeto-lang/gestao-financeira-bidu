using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class PainelService
    {
        private readonly AppDbContext _context;

        public PainelService(AppDbContext context)
        {
            _context = context;
        }

        // Razão do mês de uma conta: transporte, cada lançamento com o saldo depois dele e o total.
        // Sem ano e mês, mostra o mês do lançamento mais recente da conta.
        public async Task<Razao?> RazaoAsync(int contaId, int? ano, int? mes)
        {
            var conta = await _context.ContasBancarias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contaId);

            if (conta == null)
                return null;

            if (ano == null || mes == null)
            {
                var ultima = await UltimaDataAsync(contaId) ?? DateTime.UtcNow;
                ano = ultima.Year;
                mes = ultima.Month;
            }

            var inicio = new DateTime(ano.Value, mes.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var fim = inicio.AddMonths(1);

            // Transporte: saldo de abertura da conta + tudo o que entrou e saiu antes deste mês
            var entradasAntes = await _context.Receitas
                .Where(r => r.ContaBancariaId == contaId && r.DataRecebimento < inicio)
                .SumAsync(r => r.Valor);

            var saidasAntes = await _context.Despesas
                .Where(d => d.ContaBancariaId == contaId && d.DataPagamento < inicio)
                .SumAsync(d => d.Valor);

            var transporte = conta.SaldoAtual + entradasAntes - saidasAntes;

            var despesas = await _context.Despesas
                .AsNoTracking()
                .Include(d => d.Meta)
                .Include(d => d.CriadoPor)
                .Where(d => d.ContaBancariaId == contaId && d.DataPagamento >= inicio && d.DataPagamento < fim)
                .ToListAsync();

            var receitas = await _context.Receitas
                .AsNoTracking()
                .Include(r => r.FonteRecurso)
                .Include(r => r.CriadoPor)
                .Where(r => r.ContaBancariaId == contaId && r.DataRecebimento >= inicio && r.DataRecebimento < fim)
                .ToListAsync();

            var idsDespesas = despesas.Select(d => d.Id).ToList();
            var idsReceitas = receitas.Select(r => r.Id).ToList();

            // Primeiro comprovante de cada despesa do mês, para mostrar "NF-e 004512"
            var comprovantes = (await _context.Documentos
                    .AsNoTracking()
                    .Where(doc => idsDespesas.Contains(doc.DespesaId))
                    .ToListAsync())
                .GroupBy(doc => doc.DespesaId)
                .ToDictionary(g => g.Key, g => $"{g.First().Tipo} {g.First().Numero}");

            var despesasConciliadas = await _context.MovimentosExtrato
                .Where(m => m.DespesaId != null && idsDespesas.Contains(m.DespesaId.Value))
                .Select(m => m.DespesaId!.Value)
                .ToListAsync();

            var receitasConciliadas = await _context.MovimentosExtrato
                .Where(m => m.ReceitaId != null && idsReceitas.Contains(m.ReceitaId.Value))
                .Select(m => m.ReceitaId!.Value)
                .ToListAsync();

            // Junta despesas e receitas numa lista só, ainda sem o saldo
            var lancamentos = despesas.Select(d =>
                {
                    var temComprovante = comprovantes.TryGetValue(d.Id, out var comprovante);
                    var situacao = !temComprovante ? "sem-comprovante"
                        : despesasConciliadas.Contains(d.Id) ? "conciliado" : "a-conciliar";

                    return new LinhaRazao("despesa", d.Id, d.DataPagamento, d.Descricao,
                        $"{d.Meta?.Descricao ?? "Sem meta"} - {comprovante ?? "sem comprovante"}",
                        d.Valor, 0, 0, situacao, d.CriadoPor?.Nome);
                })
                .Concat(receitas.Select(r => new LinhaRazao("receita", r.Id, r.DataRecebimento, r.Descricao,
                    $"Receita - {r.FonteRecurso?.Nome ?? "sem fonte"}",
                    0, r.Valor, 0, receitasConciliadas.Contains(r.Id) ? "conciliado" : "a-conciliar", r.CriadoPor?.Nome)))
                .OrderBy(l => l.Data)
                .ThenBy(l => l.Tipo == "receita" ? 0 : 1)
                .ThenBy(l => l.Id);

            // Saldo corrente: cada linha parte do saldo da linha anterior
            var saldo = transporte;
            var linhas = new List<LinhaRazao>();

            foreach (var linha in lancamentos)
            {
                saldo += linha.Credito - linha.Debito;

                // "with" copia o record trocando só o que for informado
                linhas.Add(linha with { Saldo = saldo });
            }

            return new Razao(ano.Value, mes.Value, transporte,
                linhas.Sum(l => l.Credito), linhas.Sum(l => l.Debito), saldo, linhas);
        }

        // Números do menu lateral
        public async Task<Contadores> ContadoresAsync()
        {
            var semComprovante = await _context.Despesas
                .CountAsync(d => !_context.Documentos.Any(doc => doc.DespesaId == d.Id));

            var aConciliar = await _context.MovimentosExtrato
                .CountAsync(m => m.DespesaId == null && m.ReceitaId == null);

            return new Contadores(semComprovante, aConciliar);
        }

        private async Task<DateTime?> UltimaDataAsync(int contaId)
        {
            var despesa = await _context.Despesas
                .Where(d => d.ContaBancariaId == contaId)
                .MaxAsync(d => (DateTime?)d.DataPagamento);

            var receita = await _context.Receitas
                .Where(r => r.ContaBancariaId == contaId)
                .MaxAsync(r => (DateTime?)r.DataRecebimento);

            if (despesa == null) return receita;
            if (receita == null) return despesa;
            return despesa > receita ? despesa : receita;
        }
    }
}