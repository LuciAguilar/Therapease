const panelFicha = document.querySelector('#panel-ficha-paciente');
const contenidoFicha = document.querySelector('#panel-contenido-ficha');
let solicitudFicha;

document.querySelector('#resultados-pacientes').addEventListener('click', evento => {
    const enlace = evento.target.closest('a[data-abrir-ficha]');
    if (!enlace || evento.button !== 0 || evento.ctrlKey || evento.metaKey || evento.shiftKey || evento.altKey) return;

    evento.preventDefault();
    abrirFicha(enlace.href);
});

async function abrirFicha(url) {
    solicitudFicha?.abort();
    const actual = new AbortController();
    solicitudFicha = actual;
    contenidoFicha.textContent = 'Cargando ficha…';
    panelFicha.showModal();
    try {
        const respuesta = await fetch(url, { signal: actual.signal });
        if (respuesta.redirected) {
            if (!actual.signal.aborted) window.location.assign(respuesta.url);
            return;
        }
        if (!respuesta.ok && respuesta.status !== 404) throw new Error();
        const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
        if (actual.signal.aborted || !panelFicha.open) return;
        const ficha = documento.querySelector('#contenido-ficha-paciente');
        if (!ficha) throw new Error();
        contenidoFicha.replaceChildren(...ficha.childNodes);
    } catch {
        if (!actual.signal.aborted) contenidoFicha.textContent = 'No se pudo abrir la ficha.';
    }
}

panelFicha.addEventListener('close', () => {
    solicitudFicha?.abort();
    contenidoFicha.replaceChildren();
});

panelFicha.addEventListener('click', evento => {
    if (evento.target !== panelFicha) return;
    const borde = panelFicha.getBoundingClientRect();
    if (evento.clientX < borde.left) panelFicha.close();
});

if (panelFicha.dataset.fichaInicial) abrirFicha(panelFicha.dataset.fichaInicial);
