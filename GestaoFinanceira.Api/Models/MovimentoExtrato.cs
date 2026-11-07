using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestaoFinanceira.Api.Models
{
    // Uma linha do extrato do banco, importada do arquivo OFX
    public class MovimentoExtrato
    {
        [Key]
        public int Id { get; set; }

        public DateTime Data { get; set; }

        // Negativo para saídas, positivo para entradas, como vem do banco
        public decimal Valor { get; set; }

        [Required]
        [MaxLength(200)]
        public string Descricao { get; set; } = string.Empty;

        // Identificador que o banco dá ao movimento (FITID); evita importar duas vezes
        [Required]
        [MaxLength(100)]
        public string IdBanco { get; set; } = string.Empty;

        [Required]
        public int ContaBancariaId { get; set; }

        [ForeignKey("ContaBancariaId")]
        public ContaBancaria? ContaBancaria { get; set; }

        // Preenchido quando o movimento é conciliado com uma despesa ou uma receita
        public int? DespesaId { get; set; }

        [ForeignKey("DespesaId")]
        public Despesa? Despesa { get; set; }

        public int? ReceitaId { get; set; }

        [ForeignKey("ReceitaId")]
        public Receita? Receita { get; set; }
    }
}