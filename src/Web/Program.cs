using TherapEase.Web.Salud;

var indiceSalud = Array.IndexOf(args, "--salud");
if (indiceSalud >= 0)
{
    return await SondaDeSalud.EjecutarAsync(args.ElementAtOrDefault(indiceSalud + 1));
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();
app.MapHealthChecks("/salud");
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
return 0;
