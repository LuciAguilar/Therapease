window.addEventListener("pagehide", () => {
    document.documentElement.style.visibility = "hidden";
});

window.addEventListener("pageshow", (evento) => {
    const navegacion = performance.getEntriesByType("navigation")[0];
    if (evento.persisted || navegacion?.type === "back_forward") {
        const acceso = document.querySelector('meta[name="ruta-acceso"]');
        window.location.replace(acceso.content);
        return;
    }

    document.documentElement.style.visibility = "";
});
