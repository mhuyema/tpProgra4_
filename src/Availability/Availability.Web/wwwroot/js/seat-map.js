(function () {
    const map = document.querySelector('[data-seat-map]');
    if (!map) return;

    const max = parseInt(map.dataset.maxSeats, 10) || 6;
    const list = document.querySelector('[data-seat-list]');
    const total = document.querySelector('[data-seat-total]');
    const count = document.querySelector('[data-seat-count]');
    const cta = document.querySelector('[data-seat-cta] .bombo-btn');
    const inputs = document.querySelector('[data-seat-inputs]');
    const form = inputs ? inputs.closest('form') : null;
    const selected = new Map(); // id -> { label, price }

    const money = (n) => '$ ' + n.toLocaleString('es-AR');

    function render() {
        list.replaceChildren();
        if (selected.size === 0) {
            const empty = document.createElement('li');
            empty.className = 'bombo-body-sm bombo-reserva__empty';
            empty.textContent = 'Todavía no elegiste butacas.';
            list.appendChild(empty);
        }
        let sum = 0;
        if (inputs) inputs.replaceChildren();
        selected.forEach(({ label, price }, id) => {
            sum += price;
            if (inputs) {
                const hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = 'AsientoIds';
                hidden.value = id;
                inputs.appendChild(hidden);
            }
            const li = document.createElement('li');
            li.className = 'bombo-reserva__item';
            const name = document.createElement('span');
            name.className = 'bombo-label-md';
            name.textContent = 'Butaca ' + label;
            const value = document.createElement('span');
            value.className = 'bombo-body-md';
            value.textContent = money(price);
            li.append(name, value);
            list.appendChild(li);
        });
        total.textContent = money(sum);
        count.textContent = selected.size + ' / ' + max;
        if (cta) cta.disabled = selected.size === 0;
    }

    function toggle(seat) {
        const id = seat.dataset.seat;
        if (selected.has(id)) {
            selected.delete(id);
            seat.classList.remove('bombo-seat--selected');
            seat.setAttribute('aria-pressed', 'false');
        } else {
            if (selected.size >= max) return;
            selected.set(id, { label: seat.dataset.label, price: Number(seat.dataset.price) });
            seat.classList.add('bombo-seat--selected');
            seat.setAttribute('aria-pressed', 'true');
        }
    }

    map.addEventListener('click', (e) => {
        const seat = e.target.closest('.bombo-seat[data-seat]');
        if (!seat || seat.disabled) return;
        toggle(seat);
        render();
    });

    // Sin sesión, "Confirmar reserva" abre el panel que lleva a iniciar sesión en vez de enviar el formulario.
    const requiresLogin = form && form.dataset.authenticated === 'false';
    if (cta && requiresLogin) {
        cta.addEventListener('click', (e) => {
            e.preventDefault();
            if (selected.size === 0 || !window.bomboPanel) return;
            // El formulario del panel viaja con la selección para que el servidor la conserve al iniciar sesión.
            const target = document.querySelector('[data-login-seat-inputs]');
            if (target && inputs) target.replaceChildren(...Array.from(inputs.children).map((n) => n.cloneNode(true)));
            window.bomboPanel.open(form.dataset.loginPanel);
        });
    }

    // Selección guardada antes de iniciar sesión: se vuelve a marcar, salteando butacas que ya no estén libres.
    ((form && form.dataset.preselect) || '').split(',').filter(Boolean).forEach((id) => {
        const seat = map.querySelector('.bombo-seat[data-seat="' + id + '"]');
        if (seat && !seat.disabled) toggle(seat);
    });

    render();
})();
