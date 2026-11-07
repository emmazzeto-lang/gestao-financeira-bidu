using System.Text;
using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace GestaoFinanceira.Tests
{
    // O xUnit cria uma instância nova da classe para cada teste
    // e chama o Dispose no final, então cada teste tem sua própria pasta.
    public class DocumentoServiceTests : IDisposable
    {
        private readonly string _pastaTemporaria;
        private readonly AppDbContext _context;
        private readonly DocumentoService _service;

        public DocumentoServiceTests()
        {
            _pastaTemporaria = Path.Combine(Path.GetTempPath(), "testes-comprovantes", Guid.NewGuid().ToString("N"));

            _context = BancoDeTeste.Criar();

            // Despesas 1 e 2 são do mesmo fornecedor; a 3 não tem fornecedor
            _context.Despesas.Add(new Despesa { Id = 1, Descricao = "Compra 1", Valor = 100, ContaBancariaId = 1, PlanoTrabalhoId = 1, MetaId = 1, FornecedorId = 1 });
            _context.Despesas.Add(new Despesa { Id = 2, Descricao = "Compra 2", Valor = 100, ContaBancariaId = 1, PlanoTrabalhoId = 1, MetaId = 1, FornecedorId = 1 });
            _context.Despesas.Add(new Despesa { Id = 3, Descricao = "Compra 3", Valor = 100, ContaBancariaId = 1, PlanoTrabalhoId = 1, MetaId = 1 });
            _context.SaveChanges();

            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Armazenamento:PastaComprovantes"] = "Comprovantes"
                })
                .Build();

            _service = new DocumentoService(_context, new AmbienteDeTeste(_pastaTemporaria), configuracao);
        }

        public void Dispose()
        {
            _context.Dispose();

            if (Directory.Exists(_pastaTemporaria))
                Directory.Delete(_pastaTemporaria, recursive: true);
        }

        private static IFormFile Arquivo(string nome, string conteudo)
        {
            var bytes = Encoding.ASCII.GetBytes(conteudo);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Arquivo", nome);
        }

        private static EnvioDocumento Envio(int despesaId, string numero, IFormFile arquivo)
        {
            return new EnvioDocumento { DespesaId = despesaId, Tipo = "NF-e", Numero = numero, Arquivo = arquivo };
        }

        [Fact]
        public async Task PdfVerdadeiroEhGravado()
        {
            var (documento, erro) = await _service.EnviarAsync(Envio(1, "004512", Arquivo("nota.pdf", "%PDF-1.4 conteudo")));

            Assert.Null(erro);
            Assert.NotNull(documento);
            Assert.True(File.Exists(_service.CaminhoDoArquivo(documento)));
            Assert.Equal("nota.pdf", documento.NomeOriginal);
        }

        [Fact]
        public async Task ExtensaoNaoPermitidaEhRecusada()
        {
            var (_, erro) = await _service.EnviarAsync(Envio(1, "1", Arquivo("programa.exe", "MZ")));

            Assert.Equal("Envie um arquivo PDF, JPG ou PNG.", erro);
        }

        [Fact]
        public async Task ArquivoRenomeadoParaPdfEhRecusado()
        {
            var (_, erro) = await _service.EnviarAsync(Envio(1, "1", Arquivo("nota.pdf", "texto qualquer")));

            Assert.Equal("O conteúdo do arquivo não corresponde à extensão informada.", erro);
        }

        [Fact]
        public async Task MesmaNotaNoMesmoFornecedorEhRecusadaMesmoSemOsZeros()
        {
            await _service.EnviarAsync(Envio(1, "004512", Arquivo("nota.pdf", "%PDF-1.4")));

            var (documento, erro) = await _service.EnviarAsync(Envio(2, "4512", Arquivo("nota.pdf", "%PDF-1.4")));

            Assert.Null(documento);
            Assert.Equal("Este documento já comprova a despesa Nº 1.", erro);
        }

        [Fact]
        public async Task MesmoNumeroDeOutroFornecedorPassa()
        {
            await _service.EnviarAsync(Envio(1, "004512", Arquivo("nota.pdf", "%PDF-1.4")));

            var (_, erro) = await _service.EnviarAsync(Envio(3, "004512", Arquivo("nota.pdf", "%PDF-1.4")));

            Assert.Null(erro);
        }

        [Fact]
        public async Task BuscarRepetidoEncontraANotaJaUsada()
        {
            await _service.EnviarAsync(Envio(1, "004512", Arquivo("nota.pdf", "%PDF-1.4")));

            var repetido = await _service.BuscarRepetidoAsync("NF-e", "4512", 1);
            var outroFornecedor = await _service.BuscarRepetidoAsync("NF-e", "4512", null);

            Assert.Equal(1, repetido?.DespesaId);
            Assert.Null(outroFornecedor);
        }

        // Substitui o ambiente do ASP.NET: só precisamos dizer qual é a pasta raiz
        private class AmbienteDeTeste : IWebHostEnvironment
        {
            public AmbienteDeTeste(string pasta)
            {
                ContentRootPath = pasta;
            }

            public string ContentRootPath { get; set; }
            public string WebRootPath { get; set; } = string.Empty;
            public string ApplicationName { get; set; } = "Testes";
            public string EnvironmentName { get; set; } = "Testes";
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}