namespace GestaoFinanceira.Api.Models
{
    // Dados do formulário de envio; não vira tabela no banco
    public class EnvioDocumento
    {
        public int DespesaId { get; set; }

        public string? Tipo { get; set; }

        public string? Numero { get; set; }

        public IFormFile? Arquivo { get; set; }
    }
}