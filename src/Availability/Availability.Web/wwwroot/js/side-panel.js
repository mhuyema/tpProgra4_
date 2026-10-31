// Panel lateral genérico: [data-panel-open="id"] lo abre; [data-panel-close], Esc o el fondo lo cierran.
// Otros scripts pueden abrirlo con window.bomboPanel.open(id).
(function () {
    let lastFocus = null;

    function open(id) {
        const panel = document.getElementById(id);
        if (!panel) return;
        lastFocus = document.activeElement;
        panel.hidden = false;
        const first = panel.querySelector('input:not([type="hidden"])');
        (first || panel.querySelector('.bombo-panel__sheet')).focus();
    }

    function close(panel) {
        panel.hidden = true;
        if (lastFocus) lastFocus.focus();
    }

    document.addEventListener('click', (e) => {
        const opener = e.target.closest('[data-panel-open]');
        if (opener) { open(opener.dataset.panelOpen); return; }
        if (e.target.closest('[data-panel-close]')) {
            const panel = e.target.closest('[data-panel]');
            if (panel) close(panel);
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key !== 'Escape') return;
        document.querySelectorAll('[data-panel]:not([hidden])').forEach(close);
    });

    // Paneles que deben mostrarse al cargar (por ejemplo, para mostrar un error de login).
    document.querySelectorAll('[data-panel][data-open-on-load="true"]').forEach((p) => open(p.id));

    window.bomboPanel = { open };
})();
