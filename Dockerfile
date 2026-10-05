# Imagenes base fijadas por digest (AGENTS.md, regla 5). Actualizar un digest es un cambio que se revisa.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS construccion
WORKDIR /fuente

COPY global.json ./
COPY src/Domain/TherapEase.Domain.csproj src/Domain/
COPY src/Application/TherapEase.Application.csproj src/Application/
COPY src/Infrastructure/TherapEase.Infrastructure.csproj src/Infrastructure/
COPY src/Web/TherapEase.Web.csproj src/Web/
RUN dotnet restore src/Web/TherapEase.Web.csproj

COPY src/ src/
RUN dotnet publish src/Web/TherapEase.Web.csproj -c Release --no-restore -o /publicacion /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS final
WORKDIR /app
COPY --from=construccion /publicacion .

# La imagen base define APP_UID (1654); sin esta linea el proceso correria como root.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# La imagen no trae curl; la propia aplicacion sondea /salud (ver SondaDeSalud).
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
  CMD ["dotnet", "TherapEase.Web.dll", "--salud", "http://localhost:8080/salud"]

ENTRYPOINT ["dotnet", "TherapEase.Web.dll"]
