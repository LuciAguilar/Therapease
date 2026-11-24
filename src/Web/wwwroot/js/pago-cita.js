(() => {
    function prepararPago() {
        document.querySelectorAll('[data-cancelar-pago]').forEach(boton => boton.hidden = false);
    }
    new MutationObserver(prepararPago).observe(document.body, { childList: true, subtree: true });
    prepararPago();
    document.addEventListener('click', evento => {
        const cancelar = evento.target.closest('[data-cancelar-pago]');
        if (!cancelar) return;
        const formulario = cancelar.closest('form');
        formulario.reset();
        const editor = formulario.closest('details');
        if (editor) { editor.open = false; editor.querySelector('summary').focus(); }
    });
    document.addEventListener('submit', async evento => {
        const formulario = evento.target;
        if (!formulario.matches('[data-formulario-pago]')) return;
        evento.preventDefault();
        if (formulario.dataset.enviando) return;
        formulario.dataset.enviando = 'true';
        const datos = new FormData(formulario);
        const boton = formulario.querySelector('button[type="submit"]');
        boton.disabled = true;
        boton.textContent = 'Guardando…';
        formulario.setAttribute('aria-busy', 'true');
        formulario.querySelectorAll('input, [data-cancelar-pago]').forEach(control => control.disabled = true);
        const contenedor = formulario.closest('[data-contenido-detalle-cita]');
        try {
            const respuesta = await fetch(formulario.action, { method: 'POST', body: datos, signal: AbortSignal.timeout(30000) });
            if (!formulario.isConnected) return;
            if (!respuesta.ok && respuesta.status !== 404) {
                if (respuesta.status >= 500 || respuesta.status === 403 || respuesta.status === 400) {
                    const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
                    document.body.replaceChildren(...documento.body.childNodes);
                    return;
                }
                throw new Error();
            }
            const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
            const contenido = documento.querySelector('[data-contenido-detalle-cita]');
            if (!contenido) { window.location.assign(respuesta.url); return; }
            if (contenedor && formulario.isConnected) contenedor.replaceWith(contenido);
        } catch {
            if (!formulario.isConnected) return;
            formulario.removeAttribute('aria-busy');
            formulario.querySelector('[data-recargar-pago]').hidden = false;
            boton.textContent = 'Consulta el estado antes de repetir';
            formulario.querySelector('[data-aviso-pago]').textContent = 'No pudimos confirmar si se guardó; revisa el registro antes de repetir';
            // Exige consultar de nuevo antes de habilitar otro envío.
        }
    });
    document.addEventListener('click', async evento => {
        const enlace = evento.target.closest('[data-recargar-pago]');
        if (!enlace || !enlace.closest('#panel-detalle-cita') || evento.ctrlKey || evento.metaKey) return;
        evento.preventDefault();
        const contenedor = enlace.closest('[data-contenido-detalle-cita]');
        contenedor.textContent = 'Consultando estado vigente…';
        try {
            const respuesta = await fetch(enlace.href);
            if (respuesta.redirected) { window.location.assign(respuesta.url); return; }
            const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
            const detalle = documento.querySelector('[data-contenido-detalle-cita]');
            if (!detalle) throw new Error();
            if (contenedor.isConnected) contenedor.replaceWith(detalle);
        } catch { contenedor.textContent = 'No se pudo abrir la cita. Cierra el panel y vuelve a intentarlo.'; }
    });
})();
