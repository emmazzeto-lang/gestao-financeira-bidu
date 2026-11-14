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

        [Required]
        public int ContaBancariaId { get; set; }
        
        [ForeignKey("ContaBancariaId")]
        public ContaBancaria? ContaBancaria { get; set; }

        [Required]
        public int PlanoTrabalhoId { get; set; }
        
        [ForeignKey("PlanoTrabalhoId")]
        public PlanoTrabalho? PlanoTrabalho { get; set; }

        // Opcional no banco para não quebrar receitas antigas; o serviço passa a exigir.
        public int? FonteRecursoId { get; set; }

        [ForeignKey("FonteRecursoId")]
        public FonteRecurso? FonteRecurso { get; set; }


        // Auditoria: quem registrou e quando (preenchido pela API, nunca pela tela)
        public int? CriadoPorId { get; set; }

        [ForeignKey("CriadoPorId")]
        public Usuario? CriadoPor { get; set; }

        public DateTime? CriadoEm { get; set; }
    
    }
}