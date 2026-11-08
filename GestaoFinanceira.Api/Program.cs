using Microsoft.EntityFrameworkCore;
using GestaoFinanceira.Api.Data;
using System.Text.Json.Serialization;
using GestaoFinanceira.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddScoped<DespesaService>();
builder.Services.AddScoped<ReceitaService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddScoped<ConciliacaoService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<UsuarioService>();

// Login por cookie: depois de entrar, o navegador manda o cookie em toda chamada
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.Cookie.Name = "gestao.sessao";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Strict;
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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