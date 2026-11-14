using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoFinanceira.Api.Models
{
    public class Despesa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Descricao { get; set; } = string.Empty;

        public decimal Valor { get; set; }
        
        public DateTime DataPagamento { get; set; }
        
        // Mês a que a despesa se refere, que pode ser diferente do mês do pagamento.
        // Gravado como o dia 1 do mês. Opcional para não quebrar as despesas antigas.
        public DateTime? Competencia { get; set; }

        [MaxLength(30)]
        public string? FormaPagamento { get; set; }

        [Required]
        public int ContaBancariaId { get; set; }
        
        [ForeignKey("ContaBancariaId")]
        public ContaBancaria? ContaBancaria { get; set; }

        [Required]
        public int PlanoTrabalhoId { get; set; }
        
        [ForeignKey("PlanoTrabalhoId")]
        public PlanoTrabalho? PlanoTrabalho { get; set; }

        // Por enquanto opcional para não quebrar as despesas que já existem no banco.
        // No passo 3 o serviço de despesas passa a exigir a meta.
        public int? MetaId { get; set; }

        [ForeignKey("MetaId")]
        public Meta? Meta { get; set; }

        // Opcional: nem toda despesa tem fornecedor, como uma tarifa bancária
        public int? FornecedorId { get; set; }

        [ForeignKey("FornecedorId")]
        public Fornecedor? Fornecedor { get; set; }


        // Auditoria: quem registrou e quando (preenchido pela API, nunca pela tela)
        public int? CriadoPorId { get; set; }

        [ForeignKey("CriadoPorId")]
        public Usuario? CriadoPor { get; set; }

        public DateTime? CriadoEm { get; set; }
    }
}