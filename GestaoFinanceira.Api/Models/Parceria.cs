using System;
using System.ComponentModel.DataAnnotations;

namespace GestaoFinanceira.Api.Models
{
    public class Parceria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TermoFomento { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        
        public DateTime DataFim { get; set; }

        public decimal ValorTotal { get; set; }

        public bool Ativo { get; set; } = true;
    }
}