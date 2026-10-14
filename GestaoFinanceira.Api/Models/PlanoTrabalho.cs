using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoFinanceira.Api.Models
{
    public class PlanoTrabalho
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Descricao { get; set; } = string.Empty;

        public decimal ValorAprovado { get; set; }

        // Relacionamento com a Parceria que já criamos
        [Required]
        public int ParceriaId { get; set; }
        
        [ForeignKey("ParceriaId")]
        public Parceria? Parceria { get; set; }
    }
}