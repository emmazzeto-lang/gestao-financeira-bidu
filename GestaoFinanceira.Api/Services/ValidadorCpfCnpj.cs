namespace GestaoFinanceira.Api.Services
{
    // Confere os dígitos verificadores do CPF e do CNPJ.
    // Desde julho de 2026 o CNPJ também pode ter letras nas 12 primeiras posições
    // (ex.: 12.ABC.345/01DE-35); o cálculo é o mesmo, usando o código de cada caractere.
    public static class ValidadorCpfCnpj
    {
        // "12.abc.345/01de-35" vira "12ABC34501DE35"
        public static string Normalizar(string? texto)
        {
            return new string((texto ?? "").ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        }

        public static bool Valido(string documento)
        {
            if (documento.Length == 11)
                return documento.All(char.IsAsciiDigit) && CpfValido(documento);

            if (documento.Length == 14)
                return char.IsAsciiDigit(documento[12]) && char.IsAsciiDigit(documento[13]) && CnpjValido(documento);

            return false;
        }

        private static bool CpfValido(string cpf)
        {
            // 111.111.111-11 passa na conta, mas não é um CPF de verdade
            if (cpf.Distinct().Count() == 1)
                return false;

            return Digito(cpf, 9, 10) == cpf[9] - '0'
                && Digito(cpf, 10, 11) == cpf[10] - '0';
        }

        private static bool CnpjValido(string cnpj)
        {
            if (cnpj.Distinct().Count() == 1)
                return false;

            return Digito(cnpj, 12, 5) == cnpj[12] - '0'
                && Digito(cnpj, 13, 6) == cnpj[13] - '0';
        }

        // Multiplica cada caractere por um peso que vai diminuindo, soma tudo e tira o resto por 11.
        // No CPF o peso começa em 10 ou 11 e desce até 2; no CNPJ desce até 2 e volta para 9.
        private static int Digito(string documento, int quantidade, int primeiroPeso)
        {
            var soma = 0;
            var peso = primeiroPeso;

            for (var i = 0; i < quantidade; i++)
            {
                // Para números dá o próprio valor; para letras, A = 17, B = 18...
                soma += (documento[i] - '0') * peso;
                peso = peso == 2 ? 9 : peso - 1;
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}