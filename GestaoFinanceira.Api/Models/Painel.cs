namespace GestaoFinanceira.Api.Models
{
    // Formatos devolvidos pelo painel; não viram tabela

    // Situacao: conciliado, a-conciliar ou sem-comprovante
    public record LinhaRazao(
        string Tipo, int Id, DateTime Data, string Historico, string Complemento,
        decimal Debito, decimal Credito, decimal Saldo, string Situacao, string? LancadoPor);

    public record Razao(
        int Ano, int Mes, decimal Transporte, decimal Entradas, decimal Saidas, decimal SaldoFinal,
        List<LinhaRazao> Linhas);

    public record Contadores(int SemComprovante, int AConciliar);
}