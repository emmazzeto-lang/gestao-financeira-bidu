using GestaoFinanceira.Api.Data;
using GestaoFinanceira.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceira.Api.Services
{
    public class DocumentoService
    {
        private const long TamanhoMaximo = 10 * 1024 * 1024; // 10 MB

        // Os primeiros bytes de cada formato: é assim que confirmamos o tipo real do arquivo
        private static readonly Dictionary<string, (string TipoConteudo, byte[] Assinatura)> Formatos = new()
        {
            [".pdf"] = ("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
        };

        private readonly AppDbContext _context;
        private readonly string _pasta;

        public DocumentoService(
            AppDbContext context,
            IWebHostEnvironment ambiente,
            IConfiguration configuracao)
        {
            _context = context;

            var pasta = configuracao["Armazenamento:PastaComprovantes"]
                ?? "Armazenamento/Comprovantes";

            _pasta = Path.Combine(ambiente.ContentRootPath, pasta);
        }

        // Devolve o documento gravado ou a mensagem de erro
        public async Task<(Documento? Documento, string? Erro)> EnviarAsync(EnvioDocumento envio)
        {
            var despesa = await _context.Despesas
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == envio.DespesaId);

            if (despesa == null)
                return (null, "A despesa informada não existe.");

            var tipo = envio.Tipo?.Trim();
            var numero = envio.Numero?.Trim();

            if (string.IsNullOrEmpty(tipo) || tipo.Length > 50)
                return (null, "Informe o tipo do documento (até 50 caracteres).");

            if (string.IsNullOrEmpty(numero) || numero.Length > 50)
                return (null, "Informe o número do documento (até 50 caracteres).");

            var arquivo = envio.Arquivo;

            if (arquivo == null || arquivo.Length == 0)
                return (null, "Anexe o arquivo do comprovante.");

            if (arquivo.Length > TamanhoMaximo)
                return (null, "O arquivo pode ter no máximo 10 MB.");

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

            if (!Formatos.TryGetValue(extensao, out var formato))
                return (null, "Envie um arquivo PDF, JPG ou PNG.");

            if (!await TemAssinaturaAsync(arquivo, formato.Assinatura))
                return (null, "O conteúdo do arquivo não corresponde à extensão informada.");

            var repetido = await BuscarRepetidoAsync(tipo, numero, despesa.FornecedorId);

            if (repetido != null)
                return (null, $"Este documento já comprova a despesa Nº {repetido.DespesaId}.");

            Directory.CreateDirectory(_pasta);

            // Nome gerado pelo sistema: evita conflito e nome de arquivo malicioso
            var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
            var caminho = Path.Combine(_pasta, nomeArquivo);

            await using (var destino = File.Create(caminho))
            {
                await arquivo.CopyToAsync(destino);
            }

            var documento = new Documento
            {
                Tipo = tipo,
                Numero = numero,
                NomeOriginal = Path.GetFileName(arquivo.FileName),
                NomeArquivo = nomeArquivo,
                TipoConteudo = formato.TipoConteudo,
                Tamanho = arquivo.Length,
                DataEnvio = DateTime.UtcNow,
                DespesaId = despesa.Id
            };

            try
            {
                _context.Documentos.Add(documento);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Se o banco falhar, não deixa arquivo perdido na pasta
                File.Delete(caminho);
                throw;
            }

            return (documento, null);
        }

        // A mesma nota não comprova dois gastos. Dois fornecedores podem usar
        // o mesmo número, então a comparação considera também o fornecedor.
        public async Task<Documento?> BuscarRepetidoAsync(string tipo, string numero, int? fornecedorId)
        {
            tipo = tipo.Trim();
            var numeroSemZeros = numero.Trim().TrimStart('0');

            return await _context.Documentos
                .AsNoTracking()
                .Where(d => d.Tipo == tipo
                    && d.Numero.TrimStart('0') == numeroSemZeros
                    && d.Despesa!.FornecedorId == fornecedorId)
                .FirstOrDefaultAsync();
        }

        public string CaminhoDoArquivo(Documento documento)
        {
            return Path.Combine(_pasta, documento.NomeArquivo);
        }

        private static async Task<bool> TemAssinaturaAsync(IFormFile arquivo, byte[] assinatura)
        {
            var inicio = new byte[assinatura.Length];

            await using var leitura = arquivo.OpenReadStream();
            var lidos = await leitura.ReadAtLeastAsync(inicio, inicio.Length, throwOnEndOfStream: false);

            return lidos == assinatura.Length && inicio.SequenceEqual(assinatura);
        }
    }
}