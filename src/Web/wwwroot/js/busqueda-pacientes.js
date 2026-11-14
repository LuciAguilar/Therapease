const formulario = document.querySelector('#busqueda-pacientes');
const resultados = document.querySelector('#resultados-pacientes');
const estado = document.querySelector('#estado-busqueda-pacientes');
let espera;
let solicitud;

async function consultar(numeroPagina) {
    clearTimeout(espera);
    solicitud?.abort();
    const texto = formulario.elements.Texto.value.trim();
    const minimo = Number(formulario.dataset.minimo);
    // Sin texto se lista todo, paginado por el servidor; con una sola letra se espera a tener el mínimo para filtrar.
    if (texto.length > 0 && texto.length < minimo) {
        estado.textContent = `Escribe al menos ${minimo} caracteres para filtrar por nombre.`;
        return;
    }

    const actual = new AbortController();
    solicitud = actual;
    const datos = new URLSearchParams(new FormData(formulario));
    datos.set('numeroPagina', numeroPagina);

    try {
        const respuesta = await fetch(formulario.action, {
            method: 'POST',
            body: datos,
            signal: actual.signal
        });
        if (actual.signal.aborted) return;
        if (respuesta.redirected) {
            window.location.assign(respuesta.url);
            return;
        }
        if (!respuesta.ok) throw new Error();
        const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
        if (actual.signal.aborted) return;
        const contenido = documento.querySelector('#resultados-pacientes');
        if (!contenido) throw new Error();
        resultados.replaceChildren(...contenido.childNodes);
        estado.textContent = 'Resultados actualizados.';
    } catch {
        if (!actual.signal.aborted)
            estado.textContent = 'No se pudo actualizar la búsqueda. Inténtalo de nuevo.';
    }
}

formulario.addEventListener('input', () => {
    clearTimeout(espera);
    solicitud?.abort();
    espera = setTimeout(() => consultar(1), 200);
});
formulario.addEventListener('submit', evento => {
    evento.preventDefault();
    consultar(1);
});
resultados.addEventListener('submit', evento => {
    if (!evento.target.matches('[data-paginacion-pacientes]')) return;
    evento.preventDefault();
    if (evento.submitter) consultar(evento.submitter.value);
});
