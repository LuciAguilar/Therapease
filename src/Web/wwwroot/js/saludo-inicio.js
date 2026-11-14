(() => {
    const saludo = document.querySelector('#saludo-inicio');
    const movimientoReducido = window.matchMedia('(prefers-reduced-motion: reduce)');
    if (!saludo || movimientoReducido.matches) return;

    const texto = saludo.textContent;
    const letras = typeof Intl.Segmenter === 'function'
        ? Array.from(new Intl.Segmenter('es', { granularity: 'grapheme' }).segment(texto), parte => parte.segment)
        : Array.from(texto);
    const animacion = document.createElement('span');
    animacion.setAttribute('aria-hidden', 'true');
    saludo.setAttribute('aria-label', texto);
    saludo.replaceChildren(animacion);

    let posicion = 0;
    const intervalo = Math.min(65, 1800 / Math.max(1, letras.length));
    function escribir() {
        if (movimientoReducido.matches || posicion >= letras.length) {
            saludo.textContent = texto;
            saludo.removeAttribute('aria-label');
            return;
        }
        animacion.textContent += letras[posicion++];
        setTimeout(escribir, intervalo);
    }
    escribir();
})();
