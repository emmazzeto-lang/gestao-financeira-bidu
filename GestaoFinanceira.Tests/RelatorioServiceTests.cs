using System.Text;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;

namespace GestaoFinanceira.Tests
{
    public class RelatorioServiceTests
    {
        // Duas despesas na meta 1 (300 e 200); só a primeira tem comprovante
        private static AppDbContext BancoComDespesas()
        {
            var context = BancoDeTeste.Criar();

            var comNota = BancoDeTeste.NovaDespesa(valor: 300);
            comNota.Id = 1;
            var semNota = BancoDeTeste.NovaDespesa(valor: 200);
            semNota.Id = 2;
            context.Despesas.AddRange(comNota, semNota);

            context.Documentos.Add(new Documento
            {
                Tipo = "NF-e", Numero = "10", NomeOriginal = "nota.pdf", NomeArquivo = "x.pdf",
                TipoConteudo = "application/pdf", DespesaId = 1
            });

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task SomaOExecutadoEContaOsComprovantes()
        {
            using var context = BancoComDespesas();
            var service = new RelatorioService(context);

            var relatorio = await service.ExecucaoAsync(1);

            var meta = Assert.Single(relatorio!.Metas);
            Assert.Equal(1000, meta.Previsto);
            Assert.Equal(500, meta.Executado);
            Assert.Equal(2, meta.Despesas);
            Assert.Equal(1, meta.ComComprovante);
            Assert.Equal(1, relatorio.DespesasSemComprovante);
        }

        [Fact]
        public async Task ParceriaQueNaoExisteDevolveNulo()
        {
            using var context = BancoDeTeste.Criar();
            var service = new RelatorioService(context);

            Assert.Null(await service.ExecucaoAsync(99));
        }

        [Fact]
        public async Task CsvUsaPontoEVirgulaEVirgulaDecimal()
        {
            using var context = BancoComDespesas();
            var relatorio = await new RelatorioService(context).ExecucaoAsync(1);

            var csv = Encoding.UTF8.GetString(RelatorioService.ExecucaoEmCsv(relatorio!));

            Assert.Contains("Meta do plano validado;Recurso público municipal;1.000,00;500,00;500,00;50,0;2;1", csv);
        }

        [Fact]
        public async Task RastreioMostraOQueAindaFalta()
        {
            using var context = BancoComDespesas();
            var service = new RelatorioService(context);

            var etapas = await service.RastrearAsync(2);

            Assert.Equal(7, etapas!.Count);
            Assert.False(etapas.Single(e => e.Etapa == "Comprovante").Ok);
            Assert.False(etapas.Single(e => e.Etapa == "Conciliação").Ok);
            Assert.True(etapas.Single(e => e.Etapa == "Meta").Ok);
        }
    }
}