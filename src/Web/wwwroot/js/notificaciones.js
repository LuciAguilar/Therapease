(() => {
    let activa;
    let cuentaRegresiva;

    function cerrarNotificacion() {
        cuentaRegresiva?.cancel();
        if (activa?.matches(':popover-open')) activa.hidePopover();
        activa = null;
    }

    function prepararNotificaciones() {
        document.querySelectorAll('.aviso-confirmacion:not([data-notificacion-lista])').forEach(aviso => {
            aviso.dataset.notificacionLista = 'true';
            cerrarNotificacion();
            activa = aviso;
            // Una confirmación dentro de un panel debe seguir siendo interactiva sobre él.
            const panel = document.querySelector('dialog[open]');
            if (panel && !panel.contains(aviso)) panel.append(aviso);
            aviso.setAttribute('popover', 'manual');
            const cerrar = aviso.querySelector('.notificacion-cerrar');
            const barra = aviso.querySelector('.notificacion-tiempo');
            cerrar.hidden = false;
            barra.hidden = false;
            cerrar.addEventListener('click', cerrarNotificacion);
            aviso.addEventListener('pointerenter', () => {
                if (activa === aviso) cuentaRegresiva?.pause();
            });
            aviso.addEventListener('pointerleave', () => {
                if (activa === aviso && !aviso.matches(':focus-within')) cuentaRegresiva?.play();
            });
            aviso.addEventListener('focusin', () => {
                if (activa === aviso) cuentaRegresiva?.pause();
            });
            aviso.addEventListener('focusout', evento => {
                if (activa === aviso && !aviso.contains(evento.relatedTarget) && !aviso.matches(':hover')) cuentaRegresiva?.play();
            });
            aviso.showPopover();
            cuentaRegresiva = barra.animate(
                [{ transform: 'scaleX(1)' }, { transform: 'scaleX(0)' }],
                { duration: 6000, easing: 'linear', fill: 'forwards' });
            cuentaRegresiva.onfinish = cerrarNotificacion;
        });
        if (activa && (!activa.isConnected || !activa.matches(':popover-open'))) cerrarNotificacion();
    }

    new MutationObserver(prepararNotificaciones).observe(document.body, { childList: true, subtree: true });
    prepararNotificaciones();
})();
