(() => {
    let reservaPendiente;

    document.addEventListener('submit', evento => {
        const formulario = evento.target;
        if (!formulario.matches('[data-formulario-cita]')) return;
        // Buscar y elegir paciente conservan su navegación habitual.
        if (evento.submitter?.hasAttribute('formaction')) return;
        evento.preventDefault();
        if (reservaPendiente) return;

        reservaPendiente = {
            formulario,
            url: formulario.action,
            datos: new FormData(formulario),
            enviando: false
        };
        enviarReserva();
    });

    document.addEventListener('click', evento => {
        if (evento.target.closest('[data-reenviar-cita]')) enviarReserva();
    });

    async function enviarReserva() {
        if (!reservaPendiente || reservaPendiente.enviando) return;
        const reserva = reservaPendiente;
        reserva.enviando = true;
        const captura = reserva.formulario.querySelector('.cita-captura');
        const panel = reserva.formulario.querySelector('[data-recuperar-cita]');
        const estado = panel.querySelector('[data-estado-envio]');
        const acciones = panel.querySelector('[data-acciones-recuperacion]');
        captura.disabled = true;
        panel.hidden = false;
        acciones.hidden = true;
        estado.textContent = 'Enviando reserva…';
        const cancelacion = new AbortController();
        const espera = setTimeout(() => cancelacion.abort(), 30000);

        try {
            const respuesta = await fetch(reserva.url, {
                method: 'POST',
                body: reserva.datos,
                credentials: 'same-origin',
                signal: cancelacion.signal
            });
            if (respuesta.redirected) {
                window.location.assign(respuesta.url);
                return;
            }
            if (!respuesta.ok) throw new Error();
            const documento = new DOMParser().parseFromString(await respuesta.text(), 'text/html');
            const contenido = documento.querySelector('main.zona-contenido');
            if (!contenido || !contenido.querySelector('[data-formulario-cita], .cita-detalle')) throw new Error();

            const destino = reserva.formulario.closest('[data-contenido-cita]');
            const contenidoCita = contenido.querySelector('[data-contenido-cita]');
            if (!destino || !contenidoCita) throw new Error();
            if (destino.closest('#panel-nueva-cita') && contenidoCita.querySelector('.cita-detalle')) {
                reservaPendiente = null;
                window.location.assign('/Agenda');
                return;
            }
            destino.replaceChildren(...contenidoCita.childNodes);
            reservaPendiente = null;
            const aviso = destino.querySelector('[role="alert"]');
            if (aviso) {
                aviso.tabIndex = -1;
                aviso.focus();
            }
        } catch {
            // Un corte o un 500 deja el resultado incierto. Solo la persona decide si reenvía.
            estado.textContent = 'No se pudo confirmar el resultado. Comprueba la reserva o reenvía la misma. Se conservaron los datos enviados.';
            acciones.hidden = false;
        } finally {
            clearTimeout(espera);
            reserva.enviando = false;
        }
    }
})();
