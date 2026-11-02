const $ = (selector) => document.querySelector(selector);
const filters = $('#filters');
let page = 1;
let sortBy = 'dateUtc';
let sortDirection = 'desc';
let appliedFilters = new URLSearchParams();
let request;
let hasNext = false;

function showSession(authenticated) {
  request?.abort();
  $('#startup').hidden = true;
  $('#login').hidden = authenticated;
  $('#dashboard').hidden = !authenticated;
  $('#logout').hidden = !authenticated;
  $('#logs').replaceChildren();
  if (!authenticated) $('#password').focus();
}

async function api(url, options) {
  const response = await fetch(url, { credentials: 'same-origin', ...options });
  if (response.status === 401) {
    showSession(false);
    throw new Error('La sesión expiró. Ingresá nuevamente.');
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.errors ? Object.values(problem.errors).flat().join('\n')
      : problem?.detail || 'No se pudo completar la consulta. Intentá nuevamente.');
  }
  return response;
}

function cell(value, className) {
  const td = document.createElement('td');
  td.textContent = value ?? '—';
  if (className) td.className = className;
  return td;
}

function render(logs) {
  $('#logs').replaceChildren();
  for (const log of logs) {
    const row = document.createElement('tr');
    const service = cell(null);
    const badge = document.createElement('span');
    badge.className = 'badge';
    badge.textContent = log.service ?? '—';
    service.replaceChildren(badge);
    row.append(cell(new Date(log.dateUtc).toLocaleString('es-AR'), 'date'), service,
      cell(log.action), cell(log.message), cell(log.correlationId, 'identifier'), cell(log.logId, 'identifier'));
    const detailCell = cell(null);
    if (log.details != null) {
      const details = document.createElement('details');
      const summary = document.createElement('summary');
      summary.textContent = 'Ver JSON';
      const pre = document.createElement('pre');
      pre.textContent = JSON.stringify(log.details, null, 2);
      details.append(summary, pre);
      detailCell.replaceChildren(details);
    }
    row.append(detailCell);
    $('#logs').append(row);
  }
}

async function loadLogs() {
  request?.abort();
  const current = new AbortController();
  request = current;
  $('#error').textContent = '';
  $('#results').setAttribute('aria-busy', 'true');
  $('#status').textContent = 'Cargando…';
  $('#logs').replaceChildren();
  $('#empty').hidden = true;
  $('#previous').disabled = $('#next').disabled = true;
  const query = new URLSearchParams(appliedFilters);
  query.set('page', page);
  query.set('sortBy', sortBy);
  query.set('sortDirection', sortDirection);
  for (const button of document.querySelectorAll('[data-sort]')) {
    const active = button.dataset.sort === sortBy;
    button.parentElement.removeAttribute('aria-sort');
    if (active) button.parentElement.setAttribute('aria-sort', sortDirection === 'asc' ? 'ascending' : 'descending');
    button.querySelector('span').textContent = active ? (sortDirection === 'asc' ? '↑' : '↓') : '↕';
  }
  try {
    const logs = await (await api(`/logs?${query}`, { signal: current.signal })).json();
    if (current.signal.aborted) return;
    render(logs);
    hasNext = logs.length === 50;
    $('#empty').hidden = logs.length !== 0;
    $('#status').textContent = `${logs.length} registros · hora local`;
    $('#page').textContent = `Página ${page} · hasta 50 registros por página`;
    $('#previous').disabled = page === 1;
    $('#next').disabled = !hasNext;
  } catch (error) {
    if (current.signal.aborted) return;
    $('#error').textContent = error.message;
    $('#status').textContent = 'Consulta sin completar';
  } finally {
    if (request === current) $('#results').setAttribute('aria-busy', 'false');
  }
}

$('#login-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const button = event.submitter;
  button.disabled = true;
  $('#login-error').textContent = '';
  try {
    const response = await fetch('/session', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ password: $('#password').value })
    });
    if (!response.ok) throw new Error(response.status === 401 ? 'Contraseña incorrecta.'
      : response.status === 503 ? 'Falta configurar la contraseña de acceso en el servidor.' : 'No se pudo iniciar sesión.');
    $('#password').value = '';
    page = 1;
    showSession(true);
    await loadLogs();
  } catch (error) {
    $('#login-error').textContent = error.message;
  } finally { button.disabled = false; }
});

$('#logout').addEventListener('click', async () => {
  try {
    await api('/session', { method: 'DELETE' });
    showSession(false);
  } catch (error) { $('#error').textContent = error.message; }
});

filters.addEventListener('submit', (event) => {
  event.preventDefault();
  const data = new FormData(filters);
  if (data.get('startDate') && data.get('endDate') && data.get('startDate') > data.get('endDate')) {
    $('#error').textContent = 'La fecha desde debe ser anterior o igual a la fecha hasta.';
    return;
  }
  appliedFilters = new URLSearchParams();
  for (const [key, raw] of data) {
    const value = raw.trim();
    if (value) appliedFilters.set(key, key.endsWith('Date') ? new Date(value).toISOString() : value);
  }
  page = 1;
  loadLogs();
});
filters.addEventListener('reset', () => {
  appliedFilters = new URLSearchParams();
  page = 1;
  loadLogs();
});
$('#refresh').addEventListener('click', loadLogs);
$('#previous').addEventListener('click', () => { if (page > 1) { page--; loadLogs(); } });
$('#next').addEventListener('click', () => { if (hasNext) { page++; loadLogs(); } });
document.querySelectorAll('[data-sort]').forEach((button) => button.addEventListener('click', () => {
  sortDirection = sortBy === button.dataset.sort && sortDirection === 'asc' ? 'desc' : 'asc';
  sortBy = button.dataset.sort;
  page = 1;
  loadLogs();
}));

(async () => {
  try {
    const response = await fetch('/session');
    if (!response.ok && response.status !== 401) throw new Error('No se pudo verificar la sesión. Recargá la página.');
    showSession(response.ok);
    if (response.ok) await loadLogs();
  } catch (error) { $('#startup').textContent = error.message; }
})();
