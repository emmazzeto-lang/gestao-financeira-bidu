using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceira.Api.Models
{
    public class FonteRecurso
    {
        [Key]
        public int Id { get; set; }

        // Ex.: Recurso público municipal, Recursos próprios, Doações privadas
        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}