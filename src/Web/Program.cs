using System.Text.Json.Serialization;
using TherapEase.Infrastructure.Configuracion;
using TherapEase.Web.Comandos;
using TherapEase.Web.Configuracion;
using TherapEase.Web.Endpoints;
using TherapEase.Web.Salud;
using TherapEase.Web.Seguridad;

var indiceSalud = Array.IndexOf(args, "--salud");
if (indiceSalud >= 0)
{
    return await SondaDeSalud.EjecutarAsync(args.ElementAtOrDefault(indiceSalud + 1));
}

if (ComandosDeIdentidad.EsComando(args))
{
    return await ComandosDeIdentidad.EjecutarAsync(args);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();
builder.Services.AgregarPersistencia(builder.Configuration.GetConnectionString("TherapEase"));
builder.Services.AgregarIdentidad();
builder.Services.AgregarServiciosDeAplicacion();
builder.Services.AgregarSeguridadWeb();
builder.Services.ConfigureHttpJsonOptions(opciones => opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Lo que no declare otra política exige sesión válida; solo estos puntos son públicos.
app.MapStaticAssets().AllowAnonymous();
app.MapHealthChecks("/salud").AllowAnonymous();
app.MapearConsultaTecnicaDeAuditoria();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
return 0;
