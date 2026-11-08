using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class UsuarioService
    {
        private const int TamanhoMinimoSenha = 8;

        private readonly AppDbContext _context;

        // Faz o hash com PBKDF2 e um "sal" aleatório: duas senhas iguais geram hashes diferentes
        private readonly PasswordHasher<Usuario> _hasher = new();

        public UsuarioService(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExisteAlgumAsync() => _context.Usuarios.AnyAsync();

        public async Task<(Usuario? usuario, string? erro)> CriarAsync(NovoUsuario novo)
        {
            var nome = novo.Nome.Trim();
            var email = novo.Email.Trim().ToLowerInvariant();

            if (nome == "")
                return (null, "Informe o nome.");

            if (!email.Contains('@') || email.Length > 150)
                return (null, "Informe um e-mail válido.");

            var erroSenha = ConferirSenha(novo.Senha);
            if (erroSenha != null)
                return (null, erroSenha);

            if (!Perfis.Todos.Contains(novo.Perfil))
                return (null, "Perfil inválido.");

            if (await _context.Usuarios.AnyAsync(u => u.Email == email))
                return (null, "Já existe um usuário com este e-mail.");

            var usuario = new Usuario { Nome = nome, Email = email, Perfil = novo.Perfil };
            usuario.SenhaHash = _hasher.HashPassword(usuario, novo.Senha);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return (usuario, null);
        }

        // Devolve o usuário só se o e-mail existir, estiver ativo e a senha conferir
        public async Task<Usuario?> AutenticarAsync(string email, string senha)
        {
            email = email.Trim().ToLowerInvariant();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || !usuario.Ativo)
                return null;

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);

            if (resultado == PasswordVerificationResult.Failed)
                return null;

            // A biblioteca pede um hash novo quando o padrão de segurança dela muda
            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
                await _context.SaveChangesAsync();
            }

            return usuario;
        }

        public static string? ConferirSenha(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha) || senha.Length < TamanhoMinimoSenha)
                return $"A senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.";

            return null;
        }

        public static UsuarioResumo Resumo(Usuario u) => new(u.Id, u.Nome, u.Email, u.Perfil, u.Ativo);
    }
}