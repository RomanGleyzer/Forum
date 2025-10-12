import { http, dom, fmt } from './shared.js';

const AVATAR_FALLBACK = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='48' height='48'%3E%3Ccircle cx='24' cy='24' r='24' fill='%23ddd'/%3E%3Ctext x='50%25' y='54%25' font-size='20' text-anchor='middle' fill='%23666'%3F%3E%3F%3C/text%3E%3C/svg%3E";

const toFullName = (u) => `${u?.firstName ?? ''} ${u?.lastName ?? ''}`.trim();

document.addEventListener('DOMContentLoaded', async () => {
    const nameEl = dom.$('#sidebar-username');
    const avatarEl = dom.$('#sidebar-avatar');
    const composerAvatarEl = dom.$('#composer-avatar');

    if (!nameEl || !avatarEl) return;

    const { ok, json } = await http.get('/api/users/me');
    if (!ok || !json) {
        nameEl.textContent = 'Без имени';
        avatarEl.src = AVATAR_FALLBACK;
        if (composerAvatarEl) composerAvatarEl.src = AVATAR_FALLBACK;
        return;
    }

    const fullName = toFullName(json) || 'Без имени';
    nameEl.textContent = fullName;
    nameEl.title = fullName;

    if (json.avatarUrl) {
        avatarEl.src = json.avatarUrl;
        if (composerAvatarEl) composerAvatarEl.src = json.avatarUrl;
    } else {
        avatarEl.src = AVATAR_FALLBACK;
        if (composerAvatarEl) composerAvatarEl.src = AVATAR_FALLBACK;
    }

    avatarEl.alt = `Аватар: ${fullName}`;
    if (composerAvatarEl) composerAvatarEl.alt = `Аватар: ${fullName}`;

    avatarEl.addEventListener('error', () => { avatarEl.src = AVATAR_FALLBACK; }, { once: true });
    composerAvatarEl?.addEventListener('error', () => { composerAvatarEl.src = AVATAR_FALLBACK; }, { once: true });
});
