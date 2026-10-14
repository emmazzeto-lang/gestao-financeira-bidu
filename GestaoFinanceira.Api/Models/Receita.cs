using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoFinanceira.Api.Models
{
    public class Receita
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Descricao { get; set; } = string.Empty;

        public decimal Valor { get; set; }
        
        public DateTime DataRecebimento { get; set; }

        // Vínculos obrigatórios para garantir a rastreabilidade
        [Required]
        public int ContaBancariaId { get; set; }
        
        [ForeignKey("ContaBancariaId")]
        public ContaBancaria? ContaBancaria { get; set; }

        [Required]
        public int PlanoTrabalhoId { get; set; }
        
        [ForeignKey("PlanoTrabalhoId")]
        public PlanoTrabalho? PlanoTrabalho { get; set; }
    }
}