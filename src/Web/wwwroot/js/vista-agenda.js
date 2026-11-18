(() => {
    const vistas = document.querySelector('.agenda-vistas');
    if (!vistas) return;
    const actual = new URL(window.location.href);
    const periodo = actual.searchParams.get('periodo') || 'semana';
    document.querySelector('.agenda').dataset.periodo = periodo;
    if (periodo === 'mes') {
        const primero = document.querySelector('.agenda-columna');
        if (primero) primero.style.gridColumnStart = String((new Date(vistas.dataset.desde + 'T12:00:00Z').getUTCDay() + 6) % 7 + 1);
    }
    const filtros = document.querySelector('.agenda-filtros');
    const fechas = [...filtros.querySelectorAll('input[type="date"]')];
    fechas.forEach(entrada => {
        const valor = entrada.value;
        entrada.type = 'text';
        entrada.placeholder = 'dd/mm/aaaa';
        entrada.pattern = '[0-9]{2}/[0-9]{2}/[0-9]{4}';
        entrada.value = valor ? valor.split('-').reverse().join('/') : '';
    });
    filtros.addEventListener('submit', evento => {
        evento.preventDefault();
        const destino = new URL(window.location.href);
        destino.search = new URLSearchParams(new FormData(filtros));
        fechas.forEach(entrada => {
            destino.searchParams.set(entrada.name, entrada.value.split('/').reverse().join('-'));
        });
        window.location.assign(destino);
    });
    document.querySelectorAll('nav[aria-label="Navegar por periodos"] a').forEach(enlace => {
        if (periodo !== 'mes') return;
        const fecha = new Date(vistas.dataset.desde + 'T12:00:00Z');
        const direccion = enlace.getAttribute('aria-label');
        if (!direccion) return;
        fecha.setUTCDate(1);
        fecha.setUTCMonth(fecha.getUTCMonth() + (direccion === 'Periodo anterior' ? -1 : 1));
        const fin = new Date(fecha);
        fin.setUTCMonth(fin.getUTCMonth() + 1, 0);
        const destino = new URL(enlace.href);
        destino.searchParams.set('desde', fecha.toISOString().slice(0, 10));
        destino.searchParams.set('hasta', fin.toISOString().slice(0, 10));
        destino.searchParams.set('periodo', 'mes');
        enlace.href = destino;
    });
    vistas.querySelectorAll('[data-periodo]').forEach(boton => {
        boton.setAttribute('aria-pressed', String(boton.dataset.periodo === periodo));
        boton.addEventListener('click', () => {
            const fecha = new Date(vistas.dataset.desde + 'T12:00:00Z');
            if (boton.dataset.periodo === 'mes') fecha.setUTCDate(1);
            else fecha.setUTCDate(fecha.getUTCDate() - (fecha.getUTCDay() + 6) % 7);
            const fin = new Date(fecha);
            if (boton.dataset.periodo === 'mes') fin.setUTCMonth(fin.getUTCMonth() + 1, 0);
            else fin.setUTCDate(fin.getUTCDate() + 6);
            actual.searchParams.set('Desde', fecha.toISOString().slice(0, 10));
            actual.searchParams.set('Hasta', fin.toISOString().slice(0, 10));
            actual.searchParams.delete('desde');
            actual.searchParams.delete('hasta');
            actual.searchParams.set('periodo', boton.dataset.periodo);
            window.location.assign(actual);
        });
    });
    if (!actual.searchParams.has('Desde') && !actual.searchParams.has('desde') &&
        !document.querySelector('.aviso-confirmacion')) {
        vistas.querySelector('[data-periodo="semana"]').click();
    }
})();
