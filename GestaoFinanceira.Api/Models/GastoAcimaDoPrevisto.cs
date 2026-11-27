namespace GestaoFinanceira.Api.Models
{
    // Visão da tela de planos: despesa que passou o saldo da meta e quem autorizou
    public record GastoAcimaDoPrevisto(
        int DespesaId, string Descricao, decimal Valor, DateTime DataPagamento, string Meta,
        string Justificativa, string? LancadoPor, string? AutorizadoPor, DateTime? AutorizadoEm);
}
