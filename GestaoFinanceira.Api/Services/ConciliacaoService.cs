using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class ConciliacaoService
    {
        // O banco pode compensar o pagamento alguns dias depois do lançamento
        private const int DiasDeTolerancia = 5;

        private readonly AppDbContext _context;

        public ConciliacaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(ResultadoImportacao? resultado, string? erro)> ImportarAsync(ImportacaoExtrato importacao)
        {
            var arquivo = importacao.Arquivo;

            if (arquivo == null || arquivo.Length == 0)
                return (null, "Envie o arquivo OFX do extrato.");

            if (!Path.GetExtension(arquivo.FileName).Equals(".ofx", StringComparison.OrdinalIgnoreCase))
                return (null, "Envie um arquivo com extensão .ofx.");

            if (arquivo.Length > 2 * 1024 * 1024)
                return (null, "O extrato deve ter no máximo 2 MB.");

            var contaExiste = await _context.ContasBancarias
                .AnyAsync(c => c.Id == importacao.ContaBancariaId);

            if (!contaExiste)
                return (null, "A conta bancária informada não existe.");

            using var memoria = new MemoryStream();
            await arquivo.CopyToAsync(memoria);

            var (lidos, erro) = LeitorOfx.Ler(memoria.ToArray());

            if (erro != null)
                return (null, erro);

            // Importar o mesmo extrato de novo não duplica nada
            var jaImportados = await _context.MovimentosExtrato
                .Where(m => m.ContaBancariaId == importacao.ContaBancariaId)
                .Select(m => m.IdBanco)
                .ToListAsync();

            var conhecidos = new HashSet<string>(jaImportados);
            var novos = new List<MovimentoExtrato>();

            foreach (var movimento in lidos)
            {
                // Add devolve false quando o IdBanco já estava no conjunto
                if (!conhecidos.Add(movimento.IdBanco))
                    continue;

                movimento.ContaBancariaId = importacao.ContaBancariaId;
                novos.Add(movimento);
            }

            _context.MovimentosExtrato.AddRange(novos);
            await _context.SaveChangesAsync();

            var mes = lidos.Max(m => m.Data).ToString("yyyy-MM");

            return (new ResultadoImportacao(novos.Count, lidos.Count - novos.Count, mes), null);
        }

        // Coloca o extrato do mês ao lado das despesas e receitas da conta
        public async Task<Batimento> MontarBatimentoAsync(int contaId, int ano, int mes)
        {
            var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fim = inicio.AddMonths(1);

            var movimentos = await _context.MovimentosExtrato
                .AsNoTracking()
                .Include(m => m.Despesa)
                .Include(m => m.Receita)
                .Where(m => m.ContaBancariaId == contaId && m.Data >= inicio && m.Data < fim)
                .OrderBy(m => m.Data)
                .ToListAsync();

            // Despesas e receitas que já estão ligadas a algum movimento, de qualquer mês
            var despesasUsadas = await _context.MovimentosExtrato
                .Where(m => m.DespesaId != null)
                .Select(m => m.DespesaId!.Value)
                .ToListAsync();

            var receitasUsadas = await _context.MovimentosExtrato
                .Where(m => m.ReceitaId != null)
                .Select(m => m.ReceitaId!.Value)
                .ToListAsync();

            // Busca alguns dias antes e depois para achar pagamentos compensados na virada do mês
            var lancamentos = await LancamentosAsync(contaId, inicio.AddDays(-DiasDeTolerancia), fim.AddDays(DiasDeTolerancia));

            var pendentes = lancamentos
                .Where(l => l.Tipo == "despesa" ? !despesasUsadas.Contains(l.Id) : !receitasUsadas.Contains(l.Id))
                .ToList();

            var linhas = new List<LinhaBatimento>();

            foreach (var m in movimentos)
            {
                var resumo = new MovimentoResumo(m.Id, m.Data, m.Descricao, m.Valor);

                if (m.Despesa != null)
                {
                    linhas.Add(new LinhaBatimento("conciliado", resumo, ParaLancamento(m.Despesa)));
                    continue;
                }

                if (m.Receita != null)
                {
                    linhas.Add(new LinhaBatimento("conciliado", resumo, ParaLancamento(m.Receita)));
                    continue;
                }

                // Sugere o lançamento de mesmo valor com a data mais próxima
                var sugestao = pendentes
                    .Where(l => l.Valor == m.Valor && Math.Abs((l.Data - m.Data).TotalDays) <= DiasDeTolerancia)
                    .OrderBy(l => Math.Abs((l.Data - m.Data).TotalDays))
                    .FirstOrDefault();

                if (sugestao != null)
                {
                    // Um lançamento só pode ser sugerido para um movimento
                    pendentes.Remove(sugestao);
                    linhas.Add(new LinhaBatimento("sugestao", resumo, sugestao));
                }
                else
                {
                    linhas.Add(new LinhaBatimento("sem-lancamento", resumo, null));
                }
            }

            // O que sobrou no sistema dentro do mês ainda não apareceu no extrato
            foreach (var l in pendentes.Where(l => l.Data >= inicio && l.Data < fim))
                linhas.Add(new LinhaBatimento("sem-extrato", null, l));

            var totalExtrato = movimentos.Sum(m => m.Valor);
            var totalSistema = lancamentos.Where(l => l.Data >= inicio && l.Data < fim).Sum(l => l.Valor);

            return new Batimento(totalExtrato, totalSistema, linhas);
        }

        public async Task<string?> ConciliarAsync(int movimentoId, PedidoConciliacao pedido)
        {
            var movimento = await _context.MovimentosExtrato.FindAsync(movimentoId);

            if (movimento == null)
                return "Movimento do extrato não encontrado.";

            if (movimento.DespesaId != null || movimento.ReceitaId != null)
                return "Este movimento já está conciliado.";

            LancamentoSistema? lancamento = null;

            if (pedido.Tipo == "despesa")
            {
                var despesa = await _context.Despesas.FindAsync(pedido.LancamentoId);
                if (despesa != null) lancamento = ParaLancamento(despesa);
            }
            else if (pedido.Tipo == "receita")
            {
                var receita = await _context.Receitas.FindAsync(pedido.LancamentoId);
                if (receita != null) lancamento = ParaLancamento(receita);
            }

            if (lancamento == null)
                return "Lançamento não encontrado. Informe uma despesa ou uma receita.";

            if (lancamento.ContaBancariaId != movimento.ContaBancariaId)
                return "O lançamento é de outra conta bancária.";

            if (lancamento.Valor != movimento.Valor)
                return "O valor do lançamento é diferente do valor do extrato.";

            var jaUsado = pedido.Tipo == "despesa"
                ? await _context.MovimentosExtrato.AnyAsync(m => m.DespesaId == pedido.LancamentoId)
                : await _context.MovimentosExtrato.AnyAsync(m => m.ReceitaId == pedido.LancamentoId);

            if (jaUsado)
                return "Este lançamento já está conciliado com outro movimento.";

            if (pedido.Tipo == "despesa")
                movimento.DespesaId = pedido.LancamentoId;
            else
                movimento.ReceitaId = pedido.LancamentoId;

            await _context.SaveChangesAsync();

            return null;
        }

        public async Task<string?> DesfazerAsync(int movimentoId)
        {
            var movimento = await _context.MovimentosExtrato.FindAsync(movimentoId);

            if (movimento == null)
                return "Movimento do extrato não encontrado.";

            if (movimento.DespesaId == null && movimento.ReceitaId == null)
                return "Este movimento ainda não foi conciliado.";

            movimento.DespesaId = null;
            movimento.ReceitaId = null;
            await _context.SaveChangesAsync();

            return null;
        }

        private async Task<List<LancamentoSistema>> LancamentosAsync(int contaId, DateTime inicio, DateTime fim)
        {
            var despesas = await _context.Despesas
                .AsNoTracking()
                .Where(d => d.ContaBancariaId == contaId && d.DataPagamento >= inicio && d.DataPagamento < fim)
                .ToListAsync();

            var receitas = await _context.Receitas
                .AsNoTracking()
                .Where(r => r.ContaBancariaId == contaId && r.DataRecebimento >= inicio && r.DataRecebimento < fim)
                .ToListAsync();

            return despesas.Select(ParaLancamento)
                .Concat(receitas.Select(ParaLancamento))
                .OrderBy(l => l.Data)
                .ToList();
        }

        // Para o banco, a despesa é uma saída: o valor fica negativo
        private static LancamentoSistema ParaLancamento(Despesa d) =>
            new("despesa", d.Id, d.Descricao, -d.Valor, d.DataPagamento, d.ContaBancariaId);

        private static LancamentoSistema ParaLancamento(Receita r) =>
            new("receita", r.Id, r.Descricao, r.Valor, r.DataRecebimento, r.ContaBancariaId);
    }
}