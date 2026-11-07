namespace GestaoFinanceira.Api.Models
{
    // Formatos devolvidos pelos relatórios; não viram tabela

    public record LinhaExecucao(
        int MetaId, string Meta, int FonteRecursoId, string FonteRecurso,
        decimal Previsto, decimal Executado, int Despesas, int ComComprovante);

    public record RelatorioExecucao(
        string TermoFomento, string Parceria, DateTime DataInicio, DateTime DataFim,
        string? Plano, DateTime? DataValidacao,
        int DespesasSemComprovante, int MovimentosAConciliar,
        List<LinhaExecucao> Metas);

    // Ok = false quando o elo ainda falta, como um comprovante não enviado
    public record EtapaRastreio(string Etapa, string Descricao, bool Ok);
}