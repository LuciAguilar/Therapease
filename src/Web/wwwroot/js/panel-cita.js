(() => {
    const panel = document.querySelector('#panel-nueva-cita');
    const contenido = panel.querySelector('[data-panel-contenido-cita]');
    let cargando = false;
    let esperaBusqueda;
    let busqueda;
    const panelDetalle = document.querySelector('#panel-detalle-cita');
    const contenidoDetalle = panelDetalle.querySelector('[data-panel-contenido-cita]');
    let consultaDetalle;

    document.addEventListener('click', async evento => {
        const enlace = evento.target.closest('a.agenda-cita');
        if (!enlace || evento.button !== 0 || evento.ctrlKey || evento.metaKey ||
            evento.shiftKey || evento.altKey || enlace.target === '_blank') return;
        evento.preventDefault();
        consultaDetalle?.abort();
        const actual = new AbortController();
        consultaDetalle = actual;
        contenidoDetalle.textContent = 'Cargando cita…';
        if (!panelDetalle.open) panelDetalle.showModal();
        try {
            const respuesta = await fetch(enlace.href, { signal: actual.signal });
            if (actual.signal.aborted) return;
            if (respuesta.redirected) { window.location.assign(respuesta.url); return; }
            if (!respuesta.ok && respuesta.status !== 404) throw new Error();
            const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
            if (actual.signal.aborted || !panelDetalle.open) return;
            const detalle = documento.querySelector('[data-contenido-detalle-cita]');
            if (!detalle) throw new Error();
            contenidoDetalle.replaceChildren(detalle);
        } catch {
            if (!actual.signal.aborted) contenidoDetalle.textContent = 'No se pudo abrir la cita. Cierra el panel y vuelve a intentarlo.';
        }
    });
    panelDetalle.addEventListener('close', () => {
        consultaDetalle?.abort();
        contenidoDetalle.replaceChildren();
    });
    panelDetalle.addEventListener('click', evento => {
        if (evento.target !== panelDetalle) return;
        const borde = panelDetalle.getBoundingClientRect();
        if (evento.clientX < borde.left || evento.clientX > borde.right ||
            evento.clientY < borde.top || evento.clientY > borde.bottom) panelDetalle.close();
    });
    document.addEventListener('input', evento => {
        if (!evento.target.matches('[data-formulario-cita] input[name="Texto"]')) return;
        clearTimeout(esperaBusqueda);
        busqueda?.abort();
        const formulario = evento.target.form;
        const resultados = formulario.querySelector('[data-resultados-cita]');
        resultados.replaceChildren();
        if (evento.target.value.trim().length < 2) {
            resultados.textContent = 'Escribe al menos dos caracteres para buscar.';
            return;
        }
        esperaBusqueda = setTimeout(async () => {
            const actual = new AbortController();
            busqueda = actual;
            resultados.textContent = 'Buscando…';
            try {
                const url = new URL(formulario.action);
                url.searchParams.set('handler', 'Buscar');
                const respuesta = await fetch(url, {
                    method: 'POST', body: new FormData(formulario), signal: actual.signal
                });
                if (actual.signal.aborted) return;
                if (respuesta.redirected) { window.location.assign(respuesta.url); return; }
                if (!respuesta.ok) throw new Error();
                const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
                const nuevos = documento.querySelector('[data-resultados-cita]');
                if (actual.signal.aborted || !resultados.isConnected) return;
                if (!nuevos) throw new Error();
                resultados.replaceChildren(...nuevos.childNodes);
                const error = documento.querySelector('[data-formulario-cita] [role="alert"]');
                if (error) resultados.textContent = error.textContent;
            } catch {
                if (!actual.signal.aborted) resultados.textContent = 'No se pudo consultar. Vuelve a escribir para intentarlo.';
            }
        }, 250);
    });

    document.addEventListener('click', async evento => {
        const enlace = evento.target.closest('a[href]');
        if (!enlace || evento.button !== 0 || evento.ctrlKey || evento.metaKey || evento.shiftKey || evento.altKey) return;
        const url = new URL(enlace.href);
        if (url.origin !== window.location.origin || url.pathname.toLowerCase() !== '/agenda/crear') return;
        evento.preventDefault();
        clearTimeout(esperaBusqueda);
        busqueda?.abort();
        panel.showModal();
        if (contenido.querySelector('[data-formulario-cita]') || cargando) return;
        cargando = true;
        contenido.textContent = 'Cargando formulario…';
        try {
            await cargarFormulario(url);
        } catch {
            contenido.textContent = 'No se pudo abrir el formulario. Cierra el panel y vuelve a intentarlo.';
        } finally {
            cargando = false;
        }
    });

    document.addEventListener('submit', async evento => {
        const formulario = evento.target;
        if (!panel.contains(formulario) || !formulario.matches('[data-formulario-cita]') ||
            !evento.submitter?.hasAttribute('formaction')) return;
        evento.preventDefault();
        if (cargando) return;
        cargando = true;
        const boton = evento.submitter;
        const datos = new FormData(formulario, boton);
        boton.disabled = true;
        try {
            await cargarFormulario(boton.formAction, { method: 'POST', body: datos });
        } catch {
            let aviso = contenido.querySelector('[data-error-panel]');
            if (!aviso) {
                aviso = document.createElement('p');
                aviso.dataset.errorPanel = '';
                aviso.setAttribute('role', 'alert');
                contenido.prepend(aviso);
            }
            aviso.textContent = 'No se pudo completar la consulta. Vuelve a intentarlo.';
        } finally {
            boton.disabled = false;
            cargando = false;
        }
    });

    async function cargarFormulario(url, opciones = {}) {
        const respuesta = await fetch(url, opciones);
        if (respuesta.redirected) {
            window.location.assign(respuesta.url);
            return;
        }
        if (!respuesta.ok) throw new Error();
        const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
        const formulario = documento.querySelector('[data-contenido-cita]');
        if (!formulario) throw new Error();
        contenido.replaceChildren(formulario);
    }

    panel.addEventListener('close', () => {
        clearTimeout(esperaBusqueda);
        busqueda?.abort();
        if (contenido.querySelector('.cita-detalle')) window.location.reload();
    });
    panel.addEventListener('click', evento => {
        if (evento.target !== panel) return;
        const borde = panel.getBoundingClientRect();
        if (evento.clientX < borde.left || evento.clientX > borde.right ||
            evento.clientY < borde.top || evento.clientY > borde.bottom) panel.close();
    });
})();
