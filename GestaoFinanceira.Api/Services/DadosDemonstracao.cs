using System.Text;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    // Preenche um banco vazio com dados de exemplo, para a avaliação do projeto.
    // Só roda com DadosDemonstracao=true e quando ainda não existe nenhum usuário.
    public class DadosDemonstracao
    {
        public const string Senha = "Avaliacao2026";

        private readonly AppDbContext _context;
        private readonly DocumentoService _documentos;

        public DadosDemonstracao(AppDbContext context, DocumentoService documentos)
        {
            _context = context;
            _documentos = documentos;
        }

        public async Task CriarAsync()
        {
            if (await _context.Usuarios.AnyAsync())
                return;

            // Um usuário de cada perfil, todos com a mesma senha de teste
            var hasher = new PasswordHasher<Usuario>();
            var usuarios = new List<Usuario>
            {
                new() { Nome = "Avaliador Administrador", Email = "admin@avaliacao.local", Perfil = Perfis.Administrador },
                new() { Nome = "Avaliador Tesouraria", Email = "tesouraria@avaliacao.local", Perfil = Perfis.Tesouraria },
                new() { Nome = "Avaliador Diretoria", Email = "diretoria@avaliacao.local", Perfil = Perfis.Diretoria },
                new() { Nome = "Avaliador Consulta", Email = "consulta@avaliacao.local", Perfil = Perfis.Consulta }
            };
            usuarios.ForEach(u => u.SenhaHash = hasher.HashPassword(u, Senha));
            var admin = usuarios[0];

            var parceria = new Parceria
            {
                Nome = "Prefeitura Municipal - Assistência Social",
                TermoFomento = "TF 012/2025",
                DataInicio = Data(2025, 3, 1),
                DataFim = Data(2027, 2, 28),
                ValorTotal = 1_800_000m
            };

            var publico = new FonteRecurso { Nome = "Recurso público municipal" };
            var proprios = new FonteRecurso { Nome = "Recursos próprios" };
            var doacoes = new FonteRecurso { Nome = "Doações privadas" };

            var plano = new PlanoTrabalho
            {
                Descricao = "Plano 2025/2027",
                ValorAprovado = 1_800_000m,
                Parceria = parceria,
                Validado = true,
                DataValidacao = Data(2025, 3, 10)
            };

            var alimentacao = new Meta { Descricao = "Meta 1 - Alimentação", ValorPrevisto = 720_000m, PlanoTrabalho = plano, FonteRecurso = publico };
            var educacao = new Meta { Descricao = "Meta 2 - Educação", ValorPrevisto = 540_000m, PlanoTrabalho = plano, FonteRecurso = publico };
            var saude = new Meta { Descricao = "Meta 3 - Saúde", ValorPrevisto = 300_000m, PlanoTrabalho = plano, FonteRecurso = proprios };
            var obras = new Meta { Descricao = "Meta 4 - Infraestrutura", ValorPrevisto = 240_000m, PlanoTrabalho = plano, FonteRecurso = doacoes };

            var conta = new ContaBancaria { NomeBanco = "Banco do Brasil", Agencia = "1234-5", NumeroConta = "56789-0" };

            var mercado = new Fornecedor { Nome = "Mercado Bom Preço Ltda", CpfCnpj = "45871236000131" };
            var papelaria = new Fornecedor { Nome = "Papelaria Escolar Central Ltda", CpfCnpj = "21903457000186" };
            var eletrica = new Fornecedor { Nome = "Elétrica Bauru Serviços Ltda", CpfCnpj = "63019842000105" };
            var medica = new Fornecedor { Nome = "Ana Paula Ribeiro (psicóloga)", CpfCnpj = "52998224725" };

            Receita Receita(DateTime data, string descricao, decimal valor, FonteRecurso fonte) => new()
            {
                DataRecebimento = data, Descricao = descricao, Valor = valor, FonteRecurso = fonte,
                ContaBancaria = conta, PlanoTrabalho = plano, CriadoPor = admin, CriadoEm = data
            };

            Despesa Despesa(DateTime data, string descricao, decimal valor, Meta meta, Fornecedor? fornecedor, string forma) => new()
            {
                DataPagamento = data, Descricao = descricao, Valor = valor, Meta = meta, Fornecedor = fornecedor,
                Competencia = new DateTime(data.Year, data.Month, 1, 0, 0, 0, DateTimeKind.Utc), FormaPagamento = forma,
                ContaBancaria = conta, PlanoTrabalho = plano, CriadoPor = admin, CriadoEm = data
            };

            var receitas = new List<Receita>
            {
                Receita(Data(2026, 9, 1), "Repasse da 7ª parcela", 75_000m, publico),
                Receita(Data(2026, 9, 15), "Doação - campanha de setembro", 4_800m, doacoes),
                Receita(Data(2026, 10, 1), "Repasse da 8ª parcela", 75_000m, publico)
            };

            var despesas = new List<Despesa>
            {
                Despesa(Data(2026, 9, 5), "Gêneros alimentícios para o refeitório", 12_480.50m, alimentacao, mercado, "Transferência (PIX)"),
                Despesa(Data(2026, 9, 10), "Material escolar do reforço", 3_215.90m, educacao, papelaria, "Boleto"),
                Despesa(Data(2026, 9, 18), "Atendimento psicológico - setembro", 1_800m, saude, medica, "TED"),
                Despesa(Data(2026, 9, 25), "Revisão da instalação elétrica", 2_950m, obras, eletrica, "Transferência (PIX)"),
                Despesa(Data(2026, 9, 30), "Tarifa de manutenção da conta", 45.90m, obras, null, "Débito em conta"),
                Despesa(Data(2026, 10, 3), "Gêneros alimentícios para o refeitório", 11_930.20m, alimentacao, mercado, "Transferência (PIX)"),
                Despesa(Data(2026, 10, 14), "Livros para a biblioteca", 2_640m, educacao, papelaria, "Boleto")
            };

            _context.Usuarios.AddRange(usuarios);
            _context.Fornecedores.AddRange(mercado, papelaria, eletrica, medica);
            _context.Metas.AddRange(alimentacao, educacao, saude, obras);
            _context.Receitas.AddRange(receitas);
            _context.Despesas.AddRange(despesas);
            await _context.SaveChangesAsync();

            // Comprovantes: a tarifa (despesas[4]) fica sem, para mostrar a pendência
            await Comprovar(despesas[0], "NF-e", "004512");
            await Comprovar(despesas[1], "NF-e", "001877");
            await Comprovar(despesas[2], "Recibo", "0003");
            await Comprovar(despesas[3], "NFS-e", "2026118");
            await Comprovar(despesas[5], "NF-e", "004733");
            await Comprovar(despesas[6], "NF-e", "001902");

            // Extrato de setembro já importado: quase tudo conciliado, a revisão elétrica fica para conferir
            _context.MovimentosExtrato.AddRange(
                Movimento(receitas[0].DataRecebimento, 75_000m, "TED RECEBIDA PREFEITURA", "DEMO01", conta, receita: receitas[0]),
                Movimento(despesas[0].DataPagamento, -12_480.50m, "PIX ENVIADO MERCADO BOM PRECO", "DEMO02", conta, despesas[0]),
                Movimento(despesas[1].DataPagamento.AddDays(1), -3_215.90m, "PAGTO BOLETO PAPELARIA", "DEMO03", conta, despesas[1]),
                Movimento(receitas[1].DataRecebimento, 4_800m, "PIX RECEBIDO CAMPANHA", "DEMO04", conta, receita: receitas[1]),
                Movimento(despesas[2].DataPagamento, -1_800m, "TED ENVIADA ANA P RIBEIRO", "DEMO05", conta, despesas[2]),
                Movimento(despesas[3].DataPagamento, -2_950m, "PIX ENVIADO ELETRICA BAURU", "DEMO06", conta),
                Movimento(despesas[4].DataPagamento, -45.90m, "TARIFA PACOTE SERVICOS", "DEMO07", conta, despesas[4]));

            await _context.SaveChangesAsync();
        }

        private static DateTime Data(int ano, int mes, int dia) => new(ano, mes, dia, 0, 0, 0, DateTimeKind.Utc);

        private static MovimentoExtrato Movimento(DateTime data, decimal valor, string descricao, string idBanco,
            ContaBancaria conta, Despesa? despesa = null, Receita? receita = null) => new()
        {
            Data = data, Valor = valor, Descricao = descricao, IdBanco = idBanco,
            ContaBancaria = conta, Despesa = despesa, Receita = receita
        };

        // Usa o mesmo envio da tela, com um PDF gerado aqui
        private async Task Comprovar(Despesa despesa, string tipo, string numero)
        {
            var conteudo = Pdf($"{tipo} {numero} - {despesa.Valor:N2}");
            var arquivo = new FormFile(new MemoryStream(conteudo), 0, conteudo.Length, "arquivo", $"{tipo}-{numero}.pdf");

            var (_, erro) = await _documentos.EnviarAsync(new EnvioDocumento
            {
                DespesaId = despesa.Id, Tipo = tipo, Numero = numero, Arquivo = arquivo
            });

            if (erro != null)
                throw new InvalidOperationException(erro);
        }

        // PDF mínimo de uma página. Só texto sem acento, por causa da fonte padrão do PDF.
        private static byte[] Pdf(string texto)
        {
            var pagina = $"BT /F1 16 Tf 72 760 Td (Comprovante de demonstracao) Tj 0 -26 Td ({texto}) Tj ET";

            var objetos = new[]
            {
                "<</Type /Catalog /Pages 2 0 R>>",
                "<</Type /Pages /Kids [3 0 R] /Count 1>>",
                "<</Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources <</Font <</F1 5 0 R>>>>>>",
                $"<</Length {pagina.Length}>>\nstream\n{pagina}\nendstream",
                "<</Type /Font /Subtype /Type1 /BaseFont /Helvetica>>"
            };

            // O fim do PDF tem um índice com a posição, em bytes, de cada objeto
            var pdf = new StringBuilder("%PDF-1.4\n");
            var posicoes = new List<int>();

            for (var i = 0; i < objetos.Length; i++)
            {
                posicoes.Add(pdf.Length);
                pdf.Append($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
            }

            var indice = pdf.Length;
            pdf.Append($"xref\n0 {objetos.Length + 1}\n0000000000 65535 f \n");
            posicoes.ForEach(posicao => pdf.Append($"{posicao:D10} 00000 n \n"));
            pdf.Append($"trailer\n<</Size {objetos.Length + 1} /Root 1 0 R>>\nstartxref\n{indice}\n%%EOF");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }
    }
}