using System.Globalization;
using System.Text;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class RelatorioService
    {
        private readonly AppDbContext _context;

        public RelatorioService(AppDbContext context)
        {
            _context = context;
        }

        // Previsto x executado de cada meta do plano validado da parceria
        public async Task<RelatorioExecucao?> ExecucaoAsync(int parceriaId)
        {
            var parceria = await _context.Parcerias
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == parceriaId);

            if (parceria == null)
                return null;

            // Uma parceria pode ter mais de um plano; vale o último validado
            var plano = await _context.PlanosTrabalho
                .AsNoTracking()
                .Where(p => p.ParceriaId == parceriaId && p.Validado)
                .OrderByDescending(p => p.DataValidacao)
                .FirstOrDefaultAsync();

            if (plano == null)
                return new RelatorioExecucao(parceria.TermoFomento, parceria.Nome, parceria.DataInicio,
                    parceria.DataFim, null, null, 0, 0, new());

            var despesasComComprovante = _context.Documentos.Select(d => d.DespesaId);

            var metas = await _context.Metas
                .AsNoTracking()
                .Where(m => m.PlanoTrabalhoId == plano.Id)
                .OrderBy(m => m.Descricao)
                .Select(m => new LinhaExecucao(
                    m.Id,
                    m.Descricao,
                    m.FonteRecursoId,
                    m.FonteRecurso!.Nome,
                    m.ValorPrevisto,
                    _context.Despesas.Where(d => d.MetaId == m.Id).Sum(d => d.Valor),
                    _context.Despesas.Count(d => d.MetaId == m.Id),
                    _context.Despesas.Count(d => d.MetaId == m.Id && despesasComComprovante.Contains(d.Id))))
                .ToListAsync();

            var semComprovante = await _context.Despesas
                .CountAsync(d => d.PlanoTrabalhoId == plano.Id && !despesasComComprovante.Contains(d.Id));

            // A conta não está ligada à parceria, então olhamos as contas que o plano usou
            var contasDoPlano = await _context.Despesas
                .Where(d => d.PlanoTrabalhoId == plano.Id)
                .Select(d => d.ContaBancariaId)
                .Union(_context.Receitas.Where(r => r.PlanoTrabalhoId == plano.Id).Select(r => r.ContaBancariaId))
                .ToListAsync();

            var aConciliar = await _context.MovimentosExtrato
                .CountAsync(m => contasDoPlano.Contains(m.ContaBancariaId) && m.DespesaId == null && m.ReceitaId == null);

            return new RelatorioExecucao(parceria.TermoFomento, parceria.Nome, parceria.DataInicio, parceria.DataFim,
                plano.Descricao, plano.DataValidacao, semComprovante, aConciliar, metas);
        }

        // Planilha para abrir no Excel: separador ";" e vírgula decimal, como o Excel em português espera
        public static byte[] ExecucaoEmCsv(RelatorioExecucao relatorio)
        {
            var br = new CultureInfo("pt-BR");
            var csv = new StringBuilder();

            csv.AppendLine($"Execução por meta;{relatorio.TermoFomento} - {relatorio.Parceria}");
            csv.AppendLine("Meta;Fonte de recurso;Previsto;Executado;Saldo;% executado;Despesas;Com comprovante");

            foreach (var m in relatorio.Metas)
            {
                var percentual = m.Previsto == 0 ? 0 : m.Executado / m.Previsto * 100;

                csv.AppendLine(string.Join(";",
                    Celula(m.Meta), Celula(m.FonteRecurso),
                    m.Previsto.ToString("N2", br), m.Executado.ToString("N2", br),
                    (m.Previsto - m.Executado).ToString("N2", br), percentual.ToString("N1", br),
                    m.Despesas, m.ComComprovante));
            }

            // O BOM no começo faz o Excel reconhecer os acentos (UTF-8)
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        }

        // Texto com ";" ou aspas precisa ir entre aspas para não quebrar a coluna
        private static string Celula(string texto) =>
            texto.Contains(';') || texto.Contains('"') ? $"\"{texto.Replace("\"", "\"\"")}\"" : texto;

        // Do termo de fomento até o extrato: cada elo que comprova a despesa
        public async Task<List<EtapaRastreio>?> RastrearAsync(int despesaId)
        {
            var despesa = await _context.Despesas
                .AsNoTracking()
                .Include(d => d.PlanoTrabalho!).ThenInclude(p => p.Parceria)
                .Include(d => d.Meta!).ThenInclude(m => m.FonteRecurso)
                .Include(d => d.Fornecedor)
                .FirstOrDefaultAsync(d => d.Id == despesaId);

            if (despesa == null)
                return null;

            var documento = await _context.Documentos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DespesaId == despesaId);

            var movimento = await _context.MovimentosExtrato
                .AsNoTracking()
                .Include(m => m.ContaBancaria)
                .FirstOrDefaultAsync(m => m.DespesaId == despesaId);

            var plano = despesa.PlanoTrabalho!;
            var validacao = plano.DataValidacao != null ? $" - validado em {plano.DataValidacao:dd/MM/yyyy}" : "";
            var fornecedor = despesa.Fornecedor != null ? $" - {despesa.Fornecedor.Nome}" : "";

            return new List<EtapaRastreio>
            {
                new("Parceria", $"{plano.Parceria!.TermoFomento} - {plano.Parceria.Nome}", true),
                new("Plano de trabalho", plano.Descricao + validacao, plano.Validado),
                new("Fonte de recurso", despesa.Meta?.FonteRecurso?.Nome ?? "Sem fonte", despesa.Meta != null),
                new("Meta", despesa.Meta?.Descricao ?? "Sem meta", despesa.Meta != null),
                new("Lançamento", $"Nº {despesa.Id} - R$ {despesa.Valor.ToString("N2", new CultureInfo("pt-BR"))} - {despesa.DataPagamento:dd/MM/yyyy}", true),
                documento != null
                    ? new("Comprovante", $"{documento.Tipo} {documento.Numero}{fornecedor}", true)
                    : new("Comprovante", "Ainda não enviado", false),
                movimento != null
                    ? new("Conciliação", $"Extrato {movimento.ContaBancaria!.NomeBanco} de {movimento.Data:dd/MM/yyyy}", true)
                    : new("Conciliação", "Ainda não conciliado com o extrato", false)
            };
        }
    }
}