const campoBusqueda = document.querySelector("#busqueda");
const resultadosUsuarios = document.querySelector("#resultados-usuarios");
const estadoBusqueda = document.querySelector("#estado-busqueda");

let espera;
let consulta;

campoBusqueda.addEventListener("input", () => {
    clearTimeout(espera);
    consulta?.abort();
    estadoBusqueda.textContent = "";
    espera = setTimeout(buscarUsuarios, 250);
});

async function buscarUsuarios() {
    const solicitud = new AbortController();
    consulta = solicitud;
    const destino = new URL(window.location.href);
    destino.searchParams.set("busqueda", campoBusqueda.value);

    try {
        const respuesta = await fetch(destino, { signal: solicitud.signal });
        if (solicitud.signal.aborted) return;
        if (respuesta.redirected) {
            window.location.replace(respuesta.url);
            return;
        }
        if (!respuesta.ok) throw new Error();
        const html = await respuesta.text();
        if (solicitud.signal.aborted) return;
        const pagina = new DOMParser().parseFromString(html, "text/html");
        const resultados = pagina.querySelector("#resultados-usuarios");
        if (!resultados) throw new Error();
        resultadosUsuarios.replaceChildren(...resultados.childNodes);
    } catch {
        if (!solicitud.signal.aborted) {
            estadoBusqueda.textContent = "No se pudo actualizar la búsqueda.";
        }
    }
}
