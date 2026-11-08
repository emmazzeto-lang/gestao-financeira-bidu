using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Tests
{
    public class ValidadorCpfCnpjTests
    {
        [Theory]
        [InlineData("529.982.247-25")]
        [InlineData("11.222.333/0001-81")]
        [InlineData("12.ABC.345/01DE-35")]
        public void DocumentoCorretoPassa(string documento)
        {
            Assert.True(ValidadorCpfCnpj.Valido(ValidadorCpfCnpj.Normalizar(documento)));
        }

        [Theory]
        [InlineData("529.982.247-24")]
        [InlineData("11.222.333/0001-82")]
        [InlineData("111.111.111-11")]
        [InlineData("1234")]
        [InlineData("12.ABC.345/01DE-3A")]
        public void DocumentoErradoEhRecusado(string documento)
        {
            Assert.False(ValidadorCpfCnpj.Valido(ValidadorCpfCnpj.Normalizar(documento)));
        }

        [Fact]
        public void NormalizarTiraPontuacaoEPoeEmMaiusculas()
        {
            Assert.Equal("12ABC34501DE35", ValidadorCpfCnpj.Normalizar(" 12.abc.345/01de-35 "));
        }
    }
}