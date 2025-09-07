import { http, dom, fmt } from './shared.js';

const AVATAR_FALLBACK = 'images/user-default.png';

const toFullName = (u) => `${u?.lastName ?? ''} ${u?.firstName ?? ''}`.trim();

document.addEventListener('DOMContentLoaded', async () => {
    const nameEl = dom.$('#sidebar-username');
    const avatarEl = dom.$('#sidebar-avatar');
    if (!nameEl || !avatarEl) return;

    const { ok, json } = await http.get('/api/users/me');
    if (!ok || !json) {
        nameEl.textContent = 'Без имени';
        avatarEl.src = AVATAR_FALLBACK;
        return;
    }

    const fullName = toFullName(json) || 'Без имени';
    nameEl.textContent = fullName;
    nameEl.title = fullName;

    if (json.avatarUrl) {
        avatarEl.src = json.avatarUrl;
    }
    avatarEl.alt = `Аватар: ${fullName}`;

    avatarEl.addEventListener('error', () => { avatarEl.src = AVATAR_FALLBACK; });
});
