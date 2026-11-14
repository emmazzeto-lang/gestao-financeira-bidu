namespace GestaoFinanceira.Api.Models
{
    // Classes usadas só para receber e devolver dados da conciliação; não viram tabela

    public class ImportacaoExtrato
    {
        public int ContaBancariaId { get; set; }

        public IFormFile? Arquivo { get; set; }
    }

    public class PedidoConciliacao
    {
        // "despesa" ou "receita"
        public string Tipo { get; set; } = string.Empty;

        public int LancamentoId { get; set; }
    }

    public record ResultadoImportacao(int Importados, int Repetidos, string Mes);

    // Despesa ou receita vista pelo lado do banco: despesa com valor negativo
    public record LancamentoSistema(string Tipo, int Id, string Descricao, decimal Valor, DateTime Data, int ContaBancariaId);

    public record MovimentoResumo(int Id, DateTime Data, string Descricao, decimal Valor);

    // Situacao: conciliado, sugestao, sem-lancamento ou sem-extrato
    public record LinhaBatimento(string Situacao, MovimentoResumo? Movimento, LancamentoSistema? Lancamento);

    // Onde está o movimento do extrato mais antigo ainda sem lançamento ligado
    public record Pendencia(int ContaBancariaId, int Ano, int Mes);
    public record Batimento(decimal TotalExtrato, decimal TotalSistema, List<LinhaBatimento> Linhas);
}