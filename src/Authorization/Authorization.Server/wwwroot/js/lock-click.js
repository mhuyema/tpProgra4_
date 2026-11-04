// Evita dobles clics en botones de confirmación marcados con data-lock:
// tras el primer clic muestran un reloj y quedan bloqueados unos segundos.
(function () {
    const RELEASE_MS = 5000;
    const locked = new WeakSet();

    function lock(btn) {
        if (locked.has(btn)) return;
        locked.add(btn);

        const label = btn.querySelector('.bombo-btn__label') || btn;
        const original = label.textContent;
        const clock = document.createElement('i');
        clock.className = 'bombo-clock';
        clock.setAttribute('aria-hidden', 'true');

        label.textContent = btn.dataset.lockText || 'Procesando…';
        btn.prepend(clock);
        btn.classList.add('bombo-btn--locked');
        btn.setAttribute('aria-busy', 'true');
        btn.setAttribute('aria-disabled', 'true');

        setTimeout(() => {
            label.textContent = original;
            clock.remove();
            btn.classList.remove('bombo-btn--locked');
            btn.removeAttribute('aria-busy');
            btn.removeAttribute('aria-disabled');
            locked.delete(btn);
        }, RELEASE_MS);
    }

    document.addEventListener('click', (e) => {
        const btn = e.target.closest('[data-lock]');
        if (!btn) return;
        if (locked.has(btn)) {
            e.preventDefault();
            e.stopPropagation();
            return;
        }
        // Los submit se bloquean recién cuando el formulario es válido (ver abajo).
        if (btn.type !== 'submit') lock(btn);
    }, true);

    document.addEventListener('submit', (e) => {
        const btn = e.target.querySelector('[data-lock][type="submit"]');
        if (!btn) return;
        if (locked.has(btn)) {
            e.preventDefault();
            return;
        }
        // Esperar a que corran las validaciones: si cancelan el envío, no se bloquea.
        setTimeout(() => { if (!e.defaultPrevented) lock(btn); }, 0);
    }, true);
})();
