using System.Security.Claims;

namespace GestaoFinanceira.Api.Services
{
    // Método de extensão: permite escrever User.IdDoUsuario() dentro de qualquer controller
    public static class UsuarioLogado
    {
        public static int? IdDoUsuario(this ClaimsPrincipal usuario)
        {
            return int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        }
    }
}