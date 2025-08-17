import { http, dom, fmt } from './shared.js';

document.addEventListener('DOMContentLoaded', async () => {
    const nameEl = dom.$('#sidebar-username');
    const avatarEl = dom.$('#sidebar-avatar');
    if (!nameEl || !avatarEl) return;

    const { ok, json } = await http.get('/api/users/me');
    if (!ok || !json) return;

    const full = `${json.firstName ?? ''} ${json.lastName ?? ''}`.trim();
    nameEl.textContent = full || 'Без имени';
    if (json.avatarUrl) avatarEl.src = json.avatarUrl;
});
