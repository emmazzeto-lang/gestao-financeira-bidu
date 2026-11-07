using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceira.Api.Models
{
    public class Fornecedor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        // Pode chegar com pontuação (18 caracteres), mas é gravado só com números
        [Required]
        [MaxLength(18)]
        public string CpfCnpj { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}