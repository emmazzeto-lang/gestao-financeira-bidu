namespace GestaoFinanceira.Api.Models
{
    // Dados que chegam nas telas de login e de usuários; não viram tabela

    public class Login
    {
        public string Email { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        public bool ManterConectado { get; set; }
    }

    public class NovoUsuario
    {
        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        public string Perfil { get; set; } = Perfis.Consulta;
    }

    // O que a API devolve sobre um usuário: sem o hash da senha
    public record UsuarioResumo(int Id, string Nome, string Email, string Perfil, bool Ativo);
}