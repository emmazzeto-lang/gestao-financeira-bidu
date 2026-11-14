namespace GestaoFinanceira.Api.Models
{
    // Visão da tela de comprovantes: cada despesa com os documentos que a comprovam

    public record DocumentoResumo(int Id, string Tipo, string Numero, string NomeOriginal, long Tamanho);

    public record SituacaoComprovante(
        int DespesaId, string Descricao, decimal Valor, DateTime DataPagamento,
        string? Fornecedor, List<DocumentoResumo> Documentos);
}