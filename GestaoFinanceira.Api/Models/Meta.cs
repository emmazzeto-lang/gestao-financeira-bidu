using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoFinanceira.Api.Models
{
    public class Meta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Descricao { get; set; } = string.Empty;

        public decimal ValorPrevisto { get; set; }

        // Toda meta pertence a um plano de trabalho...
        [Required]
        public int PlanoTrabalhoId { get; set; }

        [ForeignKey("PlanoTrabalhoId")]
        public PlanoTrabalho? PlanoTrabalho { get; set; }

        // ...e é paga por uma fonte de recurso
        [Required]
        public int FonteRecursoId { get; set; }

        [ForeignKey("FonteRecursoId")]
        public FonteRecurso? FonteRecurso { get; set; }
    }
}