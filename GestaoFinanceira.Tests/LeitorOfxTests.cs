using System.Text;
using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Tests
{
    public class LeitorOfxTests
    {
        private const string Extrato = """
            OFXHEADER:100
            DATA:OFXSGML
            CHARSET:1252
            <OFX>
            <BANKTRANLIST>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20260917120000[-3:BRT]
            <TRNAMT>-8420.90
            <FITID>A1
            <MEMO>PIX ENVIADO ATACADAO
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20260916
            <TRNAMT>62000,00
            <FITID>A2
            <NAME>TED RECEBIDA
            </STMTTRN>
            </BANKTRANLIST>
            </OFX>
            """;

        [Fact]
        public void LeDataValorIdentificadorEDescricao()
        {
            var (movimentos, erro) = LeitorOfx.Ler(Encoding.Latin1.GetBytes(Extrato));

            Assert.Null(erro);
            Assert.Equal(2, movimentos.Count);

            Assert.Equal(new DateTime(2026, 9, 17), movimentos[0].Data.Date);
            Assert.Equal(-8420.90m, movimentos[0].Valor);
            Assert.Equal("A1", movimentos[0].IdBanco);
            Assert.Equal("PIX ENVIADO ATACADAO", movimentos[0].Descricao);
        }

        [Fact]
        public void AceitaVirgulaNoValorENameSemMemo()
        {
            var (movimentos, _) = LeitorOfx.Ler(Encoding.Latin1.GetBytes(Extrato));

            Assert.Equal(62000m, movimentos[1].Valor);
            Assert.Equal("TED RECEBIDA", movimentos[1].Descricao);
        }

        [Fact]
        public void ArquivoQueNaoEhOfxEhRecusado()
        {
            var (_, erro) = LeitorOfx.Ler(Encoding.UTF8.GetBytes("data;valor\n17/09;100"));

            Assert.Equal("O arquivo não está no formato OFX.", erro);
        }

        [Fact]
        public void MovimentoSemValorEhRecusado()
        {
            var ofx = "<OFX><STMTTRN><DTPOSTED>20260917<FITID>A1</STMTTRN></OFX>";

            var (_, erro) = LeitorOfx.Ler(Encoding.UTF8.GetBytes(ofx));

            Assert.Equal("Movimento 1 do arquivo está incompleto.", erro);
        }
    }
}