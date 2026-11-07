using System.Text;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Http;

namespace GestaoFinanceira.Tests
{
    public class ConciliacaoServiceTests
    {
        private static readonly DateTime Dia17 = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);

        // Despesa 1 de 100 em 17/09 e receita 1 de 500 em 16/09, ambas na conta 1
        private static AppDbContext BancoComLancamentos()
        {
            var context = BancoDeTeste.Criar();

            var despesa = BancoDeTeste.NovaDespesa(valor: 100);
            despesa.Id = 1;
            despesa.DataPagamento = Dia17;
            context.Despesas.Add(despesa);

            context.Receitas.Add(new Receita
            {
                Id = 1, Descricao = "Repasse", Valor = 500, DataRecebimento = Dia17.AddDays(-1),
                ContaBancariaId = 1, PlanoTrabalhoId = 1, FonteRecursoId = 1
            });

            context.SaveChanges();
            return context;
        }

        private static MovimentoExtrato Movimento(int id, decimal valor, DateTime data, string idBanco)
        {
            return new MovimentoExtrato { Id = id, Valor = valor, Data = data, IdBanco = idBanco, Descricao = "Movimento", ContaBancariaId = 1 };
        }

        private static IFormFile ArquivoOfx(string conteudo)
        {
            var bytes = Encoding.UTF8.GetBytes(conteudo);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Arquivo", "extrato.ofx");
        }

        [Fact]
        public async Task ImportarDuasVezesNaoDuplica()
        {
            using var context = BancoComLancamentos();
            var service = new ConciliacaoService(context);
            var ofx = "<OFX><STMTTRN><DTPOSTED>20260917<TRNAMT>-100.00<FITID>A1<MEMO>PIX</STMTTRN></OFX>";

            var (primeira, _) = await service.ImportarAsync(new ImportacaoExtrato { ContaBancariaId = 1, Arquivo = ArquivoOfx(ofx) });
            var (segunda, _) = await service.ImportarAsync(new ImportacaoExtrato { ContaBancariaId = 1, Arquivo = ArquivoOfx(ofx) });

            Assert.Equal(1, primeira?.Importados);
            Assert.Equal(0, segunda?.Importados);
            Assert.Equal(1, segunda?.Repetidos);
            Assert.Equal("2026-09", segunda?.Mes);
            Assert.Single(context.MovimentosExtrato);
        }

        [Fact]
        public async Task SugereLancamentoDeMesmoValorComDataProxima()
        {
            using var context = BancoComLancamentos();
            // O banco compensou a despesa dois dias depois
            context.MovimentosExtrato.Add(Movimento(1, -100, Dia17.AddDays(2), "A1"));
            context.SaveChanges();
            var service = new ConciliacaoService(context);

            var batimento = await service.MontarBatimentoAsync(1, 2026, 9);

            var linha = batimento.Linhas.Single(l => l.Movimento?.Id == 1);
            Assert.Equal("sugestao", linha.Situacao);
            Assert.Equal("despesa", linha.Lancamento?.Tipo);
            Assert.Equal(-100, linha.Lancamento?.Valor);
        }

        [Fact]
        public async Task MostraOQueFaltaDeCadaLado()
        {
            using var context = BancoComLancamentos();
            // Uma tarifa que ninguém lançou; a despesa e a receita não aparecem no extrato
            context.MovimentosExtrato.Add(Movimento(1, -42.90m, Dia17, "A1"));
            context.SaveChanges();
            var service = new ConciliacaoService(context);

            var batimento = await service.MontarBatimentoAsync(1, 2026, 9);

            Assert.Equal("sem-lancamento", batimento.Linhas.Single(l => l.Movimento != null).Situacao);
            Assert.Equal(2, batimento.Linhas.Count(l => l.Situacao == "sem-extrato"));
            Assert.Equal(-42.90m, batimento.TotalExtrato);
            Assert.Equal(400m, batimento.TotalSistema);
        }

        [Fact]
        public async Task ConciliarLigaOMovimentoADespesa()
        {
            using var context = BancoComLancamentos();
            context.MovimentosExtrato.Add(Movimento(1, -100, Dia17, "A1"));
            context.SaveChanges();
            var service = new ConciliacaoService(context);

            var erro = await service.ConciliarAsync(1, new PedidoConciliacao { Tipo = "despesa", LancamentoId = 1 });
            var batimento = await service.MontarBatimentoAsync(1, 2026, 9);

            Assert.Null(erro);
            Assert.Equal("conciliado", batimento.Linhas.Single(l => l.Movimento?.Id == 1).Situacao);
        }

        [Fact]
        public async Task ValorDiferenteNaoConcilia()
        {
            using var context = BancoComLancamentos();
            context.MovimentosExtrato.Add(Movimento(1, -99, Dia17, "A1"));
            context.SaveChanges();
            var service = new ConciliacaoService(context);

            var erro = await service.ConciliarAsync(1, new PedidoConciliacao { Tipo = "despesa", LancamentoId = 1 });

            Assert.Equal("O valor do lançamento é diferente do valor do extrato.", erro);
        }

        [Fact]
        public async Task MesmaDespesaNaoConciliaComDoisMovimentos()
        {
            using var context = BancoComLancamentos();
            context.MovimentosExtrato.Add(Movimento(1, -100, Dia17, "A1"));
            context.MovimentosExtrato.Add(Movimento(2, -100, Dia17, "A2"));
            context.SaveChanges();
            var service = new ConciliacaoService(context);

            await service.ConciliarAsync(1, new PedidoConciliacao { Tipo = "despesa", LancamentoId = 1 });
            var erro = await service.ConciliarAsync(2, new PedidoConciliacao { Tipo = "despesa", LancamentoId = 1 });

            Assert.Equal("Este lançamento já está conciliado com outro movimento.", erro);
        }

        [Fact]
        public async Task DesfazerVoltaParaSugestao()
        {
            using var context = BancoComLancamentos();
            context.MovimentosExtrato.Add(Movimento(1, 500, Dia17, "A1"));
            context.SaveChanges();
            var service = new ConciliacaoService(context);
            await service.ConciliarAsync(1, new PedidoConciliacao { Tipo = "receita", LancamentoId = 1 });

            var erro = await service.DesfazerAsync(1);
            var batimento = await service.MontarBatimentoAsync(1, 2026, 9);

            Assert.Null(erro);
            Assert.Equal("sugestao", batimento.Linhas.Single(l => l.Movimento?.Id == 1).Situacao);
        }
    }
}