(function () {
    const map = document.querySelector('[data-seat-map]');
    if (!map) return;

    const max = parseInt(map.dataset.maxSeats, 10) || 6;
    const list = document.querySelector('[data-seat-list]');
    const total = document.querySelector('[data-seat-total]');
    const count = document.querySelector('[data-seat-count]');
    const cta = document.querySelector('[data-seat-cta] .bombo-btn');
    const inputs = document.querySelector('[data-seat-inputs]');
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

    map.addEventListener('click', (e) => {
        const seat = e.target.closest('.bombo-seat[data-seat]');
        if (!seat || seat.disabled) return;
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
        render();
    });

    render();
})();
