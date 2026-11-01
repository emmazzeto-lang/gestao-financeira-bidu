using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceira.Api.Models
{
    public class ContaBancaria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NomeBanco { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Agencia { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NumeroConta { get; set; } = string.Empty;

        public decimal SaldoAtual { get; set; }

        public bool Ativo { get; set; } = true;
    }
}