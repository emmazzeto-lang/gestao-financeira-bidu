using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GestaoFinanceira.Api.Models;

namespace GestaoFinanceira.Api.Services
{
    // Lê o extrato no formato OFX, que os bancos exportam.
    // Cada movimento vem entre <STMTTRN> e </STMTTRN>, e muitas tags não têm fechamento:
    //   <STMTTRN>
    //   <DTPOSTED>20260917120000[-3:BRT]
    //   <TRNAMT>-8420.90
    //   <FITID>202609170001
    //   <MEMO>PIX ENVIADO ATACADAO
    //   </STMTTRN>
    public static class LeitorOfx
    {
        public static (List<MovimentoExtrato> movimentos, string? erro) Ler(byte[] conteudo)
        {
            var texto = Encoding.UTF8.GetString(conteudo);

            // Bancos brasileiros costumam gerar o arquivo em Windows-1252
            if (texto.Contains("CHARSET:1252"))
                texto = Encoding.Latin1.GetString(conteudo);

            if (!texto.Contains("<OFX>", StringComparison.OrdinalIgnoreCase))
                return (new(), "O arquivo não está no formato OFX.");

            var movimentos = new List<MovimentoExtrato>();
            var blocos = Regex.Matches(texto, @"<STMTTRN>(.*?)</STMTTRN>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match bloco in blocos)
            {
                var conteudoBloco = bloco.Groups[1].Value;

                var data = Campo(conteudoBloco, "DTPOSTED");
                var valor = Campo(conteudoBloco, "TRNAMT");
                var idBanco = Campo(conteudoBloco, "FITID");
                var descricao = Campo(conteudoBloco, "MEMO") ?? Campo(conteudoBloco, "NAME") ?? "Sem descrição";

                if (data == null || valor == null || idBanco == null || data.Length < 8)
                    return (new(), $"Movimento {movimentos.Count + 1} do arquivo está incompleto.");

                // Só a parte AAAAMMDD interessa; o resto é hora e fuso
                if (!DateTime.TryParseExact(data[..8], "yyyyMMdd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var dataMovimento))
                    return (new(), $"Data inválida no movimento {idBanco}.");

                // Alguns bancos usam vírgula como separador decimal
                if (!decimal.TryParse(valor.Replace(',', '.'), NumberStyles.Number | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture, out var valorMovimento))
                    return (new(), $"Valor inválido no movimento {idBanco}.");

                movimentos.Add(new MovimentoExtrato
                {
                    Data = DateTime.SpecifyKind(dataMovimento, DateTimeKind.Utc),
                    Valor = valorMovimento,
                    IdBanco = idBanco,
                    Descricao = descricao.Length > 200 ? descricao[..200] : descricao
                });
            }

            if (movimentos.Count == 0)
                return (new(), "Nenhum movimento encontrado no arquivo.");

            return (movimentos, null);
        }

        // Pega o texto depois de <TAG> até a próxima tag ou quebra de linha
        private static string? Campo(string bloco, string tag)
        {
            var achado = Regex.Match(bloco, $@"<{tag}>([^<\r\n]*)", RegexOptions.IgnoreCase);
            var valor = achado.Success ? achado.Groups[1].Value.Trim() : "";
            return valor == "" ? null : valor;
        }
    }
}