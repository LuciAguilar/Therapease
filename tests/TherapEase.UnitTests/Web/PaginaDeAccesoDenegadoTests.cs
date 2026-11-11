using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TherapEase.Domain.Identidad.Constantes;
using TherapEase.Web.Pages.Login;

namespace TherapEase.UnitTests.Web;

public class PaginaDeAccesoDenegadoTests
{
    [Theory]
    [InlineData("true")]
    [InlineData(null)]
    [InlineData("otro")]
    public void SesionConTemporalRedirigeAlCambioDeContrasena(string? temporal)
    {
        var pagina = CrearPagina("Cookies", temporal);

        var resultado = Assert.IsType<RedirectToPageResult>(pagina.OnGet());

        Assert.Equal("/Cuenta/Contrasena", resultado.PageName);
    }

    [Theory]
    [InlineData("Cookies", "false")]
    [InlineData(null, null)]
    [InlineData(null, "true")]
    public void SinSesionAutenticadaConTemporalMuestraAccesoDenegado(string? autenticacion, string? temporal)
    {
        var pagina = CrearPagina(autenticacion, temporal);

        Assert.IsType<PageResult>(pagina.OnGet());
        Assert.Equal(StatusCodes.Status403Forbidden, pagina.Response.StatusCode);
    }

    private static DenegadoModel CrearPagina(string? autenticacion, string? temporal)
    {
        var identidad = new ClaimsIdentity(autenticacion);
        if (temporal is not null)
        {
            identidad.AddClaim(new Claim(Reclamaciones.DebeCambiarContrasena, temporal));
        }

        return new DenegadoModel
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
            }
        };
    }
}
