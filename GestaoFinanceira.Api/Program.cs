using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Data;
using System.Text.Json.Serialization;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// No computador a conexão vem do appsettings.Development.json; no servidor, de uma variável de ambiente
var conexao = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure a conexão em ConnectionStrings__DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conexao));

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddScoped<DespesaService>();
builder.Services.AddScoped<ReceitaService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddScoped<ConciliacaoService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<PainelService>();
// Chaves que protegem o cookie de login. Ficam gravadas junto com os comprovantes,
// para ninguém ser desconectado quando a API reinicia ou é atualizada.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Armazenamento", "Chaves")));
// Login por cookie: depois de entrar, o navegador manda o cookie em toda chamada
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "gestao.sessao";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Strict;
        // No servidor o cookie só trafega por HTTPS
        opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;

        // Numa API não existe página de login para redirecionar: responde 401 e 403
        opcoes.Events.OnRedirectToLogin = contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        opcoes.Events.OnRedirectToAccessDenied = contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        // A cada chamada confere no banco se o usuário continua ativo e com o mesmo perfil.
        // Assim, desativar alguém ou trocar o perfil vale na hora, sem esperar o cookie vencer.
        opcoes.Events.OnValidatePrincipal = async contexto =>
        {
            var id = contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var perfil = contexto.Principal?.FindFirstValue(ClaimTypes.Role);
            var banco = contexto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            var continuaValido = int.TryParse(id, out var usuarioId) && await banco.Usuarios
                .AnyAsync(u => u.Id == usuarioId && u.Ativo && u.Perfil == perfil);

            if (!continuaValido)
            {
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// Toda rota da API exige login, a não ser que tenha [AllowAnonymous]
builder.Services.AddAuthorization(opcoes =>
{
    opcoes.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
// No servidor o Caddy recebe o HTTPS e repassa para a API. Estes cabeçalhos
// contam à API o endereço de quem acessou e que a conexão original era HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
{
    opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // O Caddy fica em outro contêiner da rede interna do Docker, com endereço que muda
    opcoes.KnownIPNetworks.Clear();
    opcoes.KnownProxies.Clear();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Recuperação de acesso, rodada no servidor (não existe pela tela):
//   dotnet run -- redefinir-senha email@fundacao.org NovaSenha123
if (args.Length == 3 && args[0] == "redefinir-senha")
{
    using var escopo = app.Services.CreateScope();
    var usuarios = escopo.ServiceProvider.GetRequiredService<UsuarioService>();

    var erro = await usuarios.RedefinirSenhaPorEmailAsync(args[1], args[2]);
    Console.WriteLine(erro ?? $"Senha de {args[1]} redefinida. O usuário também foi reativado.");
    return;
}

// No servidor o banco é atualizado sozinho ao iniciar.
// No computador continue usando: dotnet ef database update
if (!app.Environment.IsDevelopment())
{
    using var escopo = app.Services.CreateScope();
    await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();