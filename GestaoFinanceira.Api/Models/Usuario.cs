using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GestaoFinanceira.Api.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        // Guardado sempre em minúsculas, para o login não depender de maiúsculas
        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        // JsonIgnore: mesmo que alguém devolva um Usuario na API, o hash não sai no JSON
        [JsonIgnore]

        // Nunca a senha em si: só o resultado do hash, que não dá para desfazer
        [Required]
        [MaxLength(200)]
        public string SenhaHash { get; set; } = string.Empty;

        // Um dos valores de Perfis: Administrador, Diretoria, Tesouraria ou Consulta
        [Required]
        [MaxLength(30)]
        public string Perfil { get; set; } = Perfis.Consulta;

        public bool Ativo { get; set; } = true;
    }
}