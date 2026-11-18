(() => {
    let abierto;
    const formatoFecha = fecha => fecha.toISOString().slice(0, 10);
    const hoy = () => new Date(new Date().toLocaleDateString('sv-SE', {
        timeZone: 'America/Hermosillo'
    }) + 'T12:00:00Z');

    function boton(texto, etiqueta, accion) {
        const elemento = document.createElement('button');
        elemento.type = 'button';
        elemento.textContent = texto;
        elemento.setAttribute('aria-label', etiqueta);
        elemento.addEventListener('click', accion);
        return elemento;
    }

    function cerrar(devolverFoco = false) {
        if (!abierto) return;
        const { panel, disparador } = abierto;
        panel.remove();
        disparador.setAttribute('aria-expanded', 'false');
        abierto = null;
        if (devolverFoco && disparador.isConnected) disparador.focus();
    }

    function aplicar(entrada, valor) {
        entrada.value = valor;
        entrada.dispatchEvent(new Event('input', { bubbles: true }));
        entrada.dispatchEvent(new Event('change', { bubbles: true }));
        cerrar(true);
    }

    function calendario(entrada, panel) {
        const fechaHoy = hoy();
        const fecha = entrada.value ? new Date(entrada.value + 'T12:00:00Z') : new Date(fechaHoy);
        fecha.setUTCDate(1);
        const dibujar = () => {
            panel.replaceChildren();
            const cabecera = document.createElement('div');
            cabecera.className = 'cita-calendario-cabecera';
            const anterior = boton('‹', 'Mes anterior', () => {
                fecha.setUTCMonth(fecha.getUTCMonth() - 1);
                dibujar();
            });
            const siguiente = boton('›', 'Mes siguiente', () => {
                fecha.setUTCMonth(fecha.getUTCMonth() + 1);
                dibujar();
            });
            anterior.disabled = fecha.getUTCFullYear() === 1 && fecha.getUTCMonth() === 0;
            siguiente.disabled = fecha.getUTCFullYear() === 9999 && fecha.getUTCMonth() === 11;
            const mes = document.createElement('select');
            mes.setAttribute('aria-label', 'Mes');
            for (let numero = 0; numero < 12; numero++) {
                const muestra = new Date(fecha);
                muestra.setUTCMonth(numero);
                mes.add(new Option(muestra.toLocaleDateString('es-MX', { month: 'long', timeZone: 'UTC' }), numero));
            }
            mes.value = fecha.getUTCMonth();
            mes.onchange = () => { fecha.setUTCMonth(Number(mes.value)); dibujar(); };
            const anio = document.createElement('input');
            anio.type = 'number';
            anio.min = '1';
            anio.max = '9999';
            anio.value = fecha.getUTCFullYear();
            anio.setAttribute('aria-label', 'Año');
            anio.onchange = () => {
                if (!anio.checkValidity() || !anio.value) return;
                fecha.setUTCFullYear(Number(anio.value));
                dibujar();
            };
            cabecera.append(anterior, mes, anio, siguiente);
            const dias = document.createElement('div');
            dias.className = 'cita-calendario-dias';
            ['Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'Sá', 'Do'].forEach(nombre => {
                const etiqueta = document.createElement('span');
                etiqueta.textContent = nombre;
                dias.append(etiqueta);
            });
            const limite = new Date(fecha);
            limite.setUTCMonth(limite.getUTCMonth() + 1, 0);
            for (let numero = 1; numero <= limite.getUTCDate(); numero++) {
                const dia = new Date(fecha);
                dia.setUTCDate(numero);
                const valor = formatoFecha(dia);
                const etiqueta = dia.toLocaleDateString('es-MX', { dateStyle: 'full', timeZone: 'UTC' });
                const elegir = boton(String(numero), etiqueta, () => aplicar(entrada, valor));
                elegir.setAttribute('aria-pressed', String(entrada.value === valor));
                if (valor === formatoFecha(fechaHoy)) elegir.setAttribute('aria-current', 'date');
                if (numero === 1) elegir.style.gridColumnStart = String((fecha.getUTCDay() + 6) % 7 + 1);
                dias.append(elegir);
            }
            const pie = document.createElement('div');
            pie.className = 'fila';
            pie.append(boton('Hoy', 'Elegir hoy en Hermosillo', () => aplicar(entrada, formatoFecha(fechaHoy))),
                boton('Cerrar', 'Cerrar calendario', () => cerrar(true)));
            panel.append(cabecera, dias, pie);
        };
        dibujar();
    }

    function horario(entrada, panel) {
        const titulo = document.createElement('p');
        titulo.className = 'cita-selector-titulo';
        titulo.textContent = entrada.name.endsWith('HoraInicio') ? 'Hora de inicio' : 'Hora de fin';
        const columnas = document.createElement('div');
        columnas.className = 'cita-horarios';
        const partes = (entrada.value || '').split(':');
        const selectores = ['Hora', 'Minutos'].map((nombre, indice) => {
            const etiqueta = document.createElement('label');
            etiqueta.textContent = nombre;
            const selector = document.createElement('select');
            selector.setAttribute('aria-label', nombre);
            selector.size = 5;
            for (let numero = 0; numero < (indice === 0 ? 24 : 60); numero++) {
                const valor = String(numero).padStart(2, '0');
                selector.add(new Option(valor, valor));
            }
            selector.value = partes[indice] || (indice === 0 ? '09' : '00');
            etiqueta.append(selector);
            columnas.append(etiqueta);
            return selector;
        });
        const aviso = document.createElement('p');
        aviso.className = 'cita-ayuda';
        aviso.textContent = 'Formato de 24 horas · Hora de Hermosillo';
        const acciones = document.createElement('div');
        acciones.className = 'acciones';
        acciones.append(
            boton('Aplicar hora', 'Aplicar hora seleccionada', () => aplicar(entrada, selectores.map(selector => selector.value).join(':'))),
            boton('Cancelar', 'Cancelar selección de hora', () => cerrar(true)));
        panel.append(titulo, columnas, aviso, acciones);
    }

    function preparar() {
        document.querySelectorAll('input.cita-selector:not([data-selector-listo])').forEach(entrada => {
            entrada.dataset.selectorListo = '';
            const contenedor = document.createElement('div');
            contenedor.className = 'cita-control-selector';
            entrada.before(contenedor);
            contenedor.append(entrada);
            const esFecha = entrada.type === 'date';
            const abrir = () => {
                    const estabaAbierto = abierto?.disparador === entrada;
                    cerrar();
                    if (estabaAbierto) return;
                    const panel = document.createElement('div');
                    panel.className = 'cita-calendario cita-selector-panel';
                    panel.setAttribute('role', 'group');
                    panel.setAttribute('aria-label', esFecha ? 'Calendario de la cita' : 'Selector de hora');
                    abierto = { panel, disparador: entrada };
                    entrada.setAttribute('aria-expanded', 'true');
                    if (esFecha) calendario(entrada, panel);
                    else horario(entrada, panel);
                    contenedor.append(panel);
                    panel.querySelector('[aria-pressed="true"], select, button')?.focus();
                };
            entrada.setAttribute('aria-expanded', 'false');
            entrada.addEventListener('click', evento => {
                evento.preventDefault();
                if (abierto?.disparador !== entrada) abrir();
            });
            entrada.addEventListener('keydown', evento => {
                if (evento.key === ' ' || (evento.altKey && evento.key === 'ArrowDown')) {
                    evento.preventDefault();
                    if (abierto?.disparador !== entrada) abrir();
                }
            });
        });
        if (abierto && !abierto.disparador.isConnected) cerrar();
    }

    document.addEventListener('click', evento => {
        if (abierto && !evento.composedPath().includes(abierto.panel) &&
            evento.target !== abierto.disparador &&
            evento.target !== abierto.disparador.parentElement.querySelector('input.cita-selector')) cerrar();
    });
    document.addEventListener('keydown', evento => {
        if (evento.key === 'Escape' && abierto) {
            evento.preventDefault();
            evento.stopPropagation();
            cerrar(true);
        }
    }, true);
    document.querySelector('#panel-nueva-cita')?.addEventListener('close', () => cerrar());
    new MutationObserver(preparar).observe(document.body, { childList: true, subtree: true });
    preparar();
})();
