namespace GestaoFinanceira.Api.Models
{
    // Os perfis ficam num lugar só, para não espalhar texto solto pelo código
    public static class Perfis
    {
        public const string Administrador = "Administrador";
        public const string Diretoria = "Diretoria";
        public const string Tesouraria = "Tesouraria";
        public const string Consulta = "Consulta";

        public static readonly string[] Todos = { Administrador, Diretoria, Tesouraria, Consulta };
    }
}