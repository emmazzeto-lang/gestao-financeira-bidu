using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoFinanceira.Api.Models
{
    public class Documento
    {
        [Key]
        public int Id { get; set; }

        // Ex.: Nota fiscal eletrônica, Recibo, Cupom fiscal
        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Numero { get; set; } = string.Empty;

        // Nome que o usuário enviou; só para mostrar e baixar
        [Required]
        [MaxLength(255)]
        public string NomeOriginal { get; set; } = string.Empty;

        // Nome gerado pelo sistema, usado para gravar o arquivo na pasta
        [Required]
        [MaxLength(100)]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TipoConteudo { get; set; } = string.Empty;

        public long Tamanho { get; set; }

        public DateTime DataEnvio { get; set; }

        [Required]
        public int DespesaId { get; set; }

        [ForeignKey("DespesaId")]
        public Despesa? Despesa { get; set; }
    }
}