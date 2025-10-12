import { dom, http, ui, storage } from './shared.js';
import { renderPost } from './post.js';

const AVATAR_FALLBACK =
    "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='120'%3E%3Ccircle cx='60' cy='60' r='60' fill='%23ddd'/%3E%3Ctext x='50%25' y='58%25' font-size='28' text-anchor='middle' fill='%23666'%3F%3E%3F%3C/text%3E%3C/svg%3E";

let currentUserProfile = null;
let paging = { skip: 0, take: 10, busy: false, eof: false };

function fullName(u) {
    return (
        [u?.firstName, u?.lastName].filter(Boolean).join(' ').trim() ||
        u?.displayName ||
        u?.username ||
        u?.email ||
        'Без имени'
    );
}

function splitDisplayName(name) {
    const parts = (name || '').trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return { firstName: '', lastName: '' };
    if (parts.length === 1) return { firstName: parts[0], lastName: '' };
    const firstName = parts.shift();
    const lastName = parts.join(' ');
    return { firstName, lastName };
}

function setText(sel, value) {
    const el = dom.$(sel);
    if (el) el.textContent = value ?? '—';
}

function setCount(sel, n) {
    const el = dom.$(sel);
    if (el) el.textContent = Number.isFinite(+n) ? String(n) : '0';
}

function applyAvatar(imgEl, url, alt) {
    if (!imgEl) return;
    imgEl.src = url || AVATAR_FALLBACK;
    imgEl.alt = alt ? `Аватар: ${alt}` : 'Аватар';
    imgEl.addEventListener(
        'error',
        () => {
            imgEl.src = AVATAR_FALLBACK;
        },
        { once: true }
    );
}

function isoForDateInput(v) {
    if (!v) return '';
    const d = new Date(v);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
}

function toggleBusy(elOrForm, busy) {
    if (!elOrForm) return;
    elOrForm.setAttribute('aria-busy', String(!!busy));
    elOrForm
        .querySelectorAll?.('input,select,textarea,button')
        .forEach((el) => (el.disabled = !!busy));
}

function showAlert(id, text, type = 'danger') {
    const box = dom.$(id);
    if (!box) return;
    box.textContent = text;
    box.classList.toggle('d-none', !text);
    box.classList.toggle('alert-danger', type === 'danger');
    box.classList.toggle('alert-success', type === 'success');
}

async function loadProfile() {
    const { ok, json } = await http.get('/api/users/me/profile');
    if (!ok || !json) throw new Error(json?.message || 'Ошибка загрузки профиля');

    currentUserProfile = json;

    const name = fullName(json);
    setText('#profile-username', name);
    setText('#about-name', name);
    setText('#profile-bio', json.about || '');
    setText('#about-bio', json.about || '');
    setText('#about-email', json.email || '');
    setText(
        '#about-birthdate',
        json.dateOfBirth ? new Date(json.dateOfBirth).toLocaleDateString('ru-RU') : '—'
    );

    setCount('#posts-count', json.postsCount ?? json.postCount);
    setCount('#followers-count', json.followersCount ?? json.subscribersCount);
    setCount('#following-count', json.followingCount ?? json.subscriptionsCount);

    const badge = dom.$('#profile-verified');
    if (badge) badge.classList.toggle('d-none', !(json.verified ?? json.isVerified));

    applyAvatar(dom.$('#profile-avatar'), json.avatarUrl, name);

    bindEditProfilePrefill();

    return json;
}

async function getUserPosts(userId, skip = 0, take = 10) {
    const id = encodeURIComponent(String(userId));
    const { ok, json } = await http.get(
        `/api/users/${id}/posts?skip=${skip}&take=${take}`
    );
    if (!ok) throw new Error(json?.message || 'Ошибка загрузки постов пользователя');
    return Array.isArray(json) ? json : json?.items ?? json?.data ?? json?.posts ?? [];
}

function renderProfilePosts(posts) {
    const container = dom.$('#profile-posts');
    const noPosts = dom.$('#no-posts');
    if (!container) return;

    if (posts.length) dom.hide(noPosts);
    else if (paging.skip === 0) {
        dom.show(noPosts, true);
        return;
    }

    const frag = document.createDocumentFragment();
    for (const p of posts) frag.append(renderPost(p));
    container.append(frag);

    ensureLoadMoreButton();
}

function ensureLoadMoreButton() {
    const container = dom.$('#profile-posts');
    if (!container) return;
    let btn = dom.$('#load-more-posts');
    if (paging.eof) {
        btn?.remove();
        return;
    }
    if (!btn) {
        btn = dom.create('button', 'btn btn-outline-secondary w-100 my-3');
        btn.id = 'load-more-posts';
        btn.type = 'button';
        btn.textContent = 'Ещё';
        btn.addEventListener('click', loadMorePosts);
        container.after(btn);
    }
    btn.disabled = paging.busy;
}

async function loadMorePosts() {
    if (paging.busy || paging.eof) return;
    if (!currentUserProfile?.id && !currentUserProfile?.userId) {
        ui.toast('Профиль не загружен');
        return;
    }
    paging.busy = true;
    try {
        const uid = currentUserProfile.id ?? currentUserProfile.userId;
        const posts = await getUserPosts(uid, paging.skip, paging.take);
        renderProfilePosts(posts);
        if (posts.length < paging.take) paging.eof = true;
        paging.skip += posts.length;
    } catch (err) {
        ui.toast(err?.message || 'Не удалось загрузить посты профиля');
    } finally {
        paging.busy = false;
        ensureLoadMoreButton();
    }
}

function bindAvatarPicker() {
    const avatarImg = dom.$('#profile-avatar');
    const overlay = dom.$('.change-avatar-overlay');
    const btn = dom.$('#change-avatar-btn');
    const fileInput = dom.$('#avatar-upload');

    if (!fileInput || !avatarImg) return;

    const openPicker = () => fileInput.click();

    overlay?.setAttribute('role', 'button');
    overlay?.setAttribute('tabindex', '0');
    overlay?.setAttribute('aria-label', 'Сменить аватар');
    avatarImg.setAttribute('role', 'button');
    avatarImg.setAttribute('tabindex', '0');
    avatarImg.setAttribute('aria-label', 'Сменить аватар');

    avatarImg.addEventListener('click', openPicker);
    overlay?.addEventListener('click', openPicker);
    btn?.addEventListener('click', openPicker);

    const keyHandler = (e) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            openPicker();
        }
    };
    overlay?.addEventListener('keydown', keyHandler);
    avatarImg.addEventListener('keydown', keyHandler);

    fileInput.addEventListener('change', async () => {
        const file = fileInput.files?.[0];
        if (!file) return;

        if (!file.type.startsWith('image/')) {
            ui.toast('Пожалуйста, выберите файл изображения');
            fileInput.value = '';
            return;
        }

        const modalSpinner = dom.$('#avatar-spinner');
        dom.show(modalSpinner, true);

        try {
            const tmpUrl = URL.createObjectURL(file);
            avatarImg.src = tmpUrl;

            const form = new FormData();
            form.append('file', file);

            const token = storage.getToken();
            const res = await fetch('/api/users/me/avatar', {
                method: 'POST',
                headers: token ? { Authorization: `Bearer ${token}` } : undefined,
                body: form,
            });

            const data = await (async () => {
                try {
                    return await res.json();
                } catch {
                    return null;
                }
            })();

            if (!res.ok) {
                applyAvatar(avatarImg, currentUserProfile?.avatarUrl, fullName(currentUserProfile));
                ui.toast(data?.message || 'Не удалось загрузить аватар');
                return;
            }

            const newUrl =
                data?.avatarUrl || data?.url || currentUserProfile?.avatarUrl || avatarImg.src;

            currentUserProfile = { ...(currentUserProfile || {}), avatarUrl: newUrl };
            applyAvatar(avatarImg, newUrl, fullName(currentUserProfile));
            ui.toast('Аватар обновлён', 'success');
        } catch (err) {
            applyAvatar(avatarImg, currentUserProfile?.avatarUrl, fullName(currentUserProfile));
            ui.toast('Не удалось загрузить аватар');
        } finally {
            dom.hide(modalSpinner);
            try {
                const src = avatarImg.getAttribute('src');
                if (src?.startsWith('blob:')) URL.revokeObjectURL(src);
            } catch { }
            fileInput.value = '';
        }
    });
}

function bindEditProfilePrefill() {
    const modal = document.getElementById('editProfileModal');
    if (!modal) return;

    modal.addEventListener(
        'show.bs.modal',
        () => {
            const p = currentUserProfile || {};
            const displayName = fullName(p);
            const email = p.email || '';
            const dob = isoForDateInput(p.dateOfBirth);
            const about = p.about || '';

            const fDisplayName = dom.$('#displayName', modal);
            const fEmail = dom.$('#email', modal);
            const fDob = dom.$('#dateOfBirth', modal);
            const fAbout = dom.$('#about', modal);

            if (fDisplayName) fDisplayName.value = displayName;
            if (fEmail) fEmail.value = email;
            if (fDob) fDob.value = dob;
            if (fAbout) fAbout.value = about;

            showAlert('#update-user-error', '');
            showAlert('#update-user-success', '');
        },
        { passive: true }
    );

    const form = dom.$('#update-user-form', modal);
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        toggleBusy(form, true);
        showAlert('#update-user-error', '');
        showAlert('#update-user-success', '');

        const displayName = dom.$('#displayName', form)?.value?.trim() || '';
        const email = dom.$('#email', form)?.value?.trim() || '';
        const dateOfBirth = dom.$('#dateOfBirth', form)?.value || '';
        const about = dom.$('#about', form)?.value?.trim() || '';

        const { firstName, lastName } = splitDisplayName(displayName);

        const payload = {
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            About: about,
            DateOfBirth: dateOfBirth,
        };

        const { ok, json } = await http.put('/api/users', payload);

        if (!ok) {
            showAlert('#update-user-error', json?.message || 'Не удалось сохранить данные профиля', 'danger');
            toggleBusy(form, false);
            return;
        }

        currentUserProfile = { ...(currentUserProfile || {}), ...json };
        const name = fullName(currentUserProfile);
        setText('#profile-username', name);
        setText('#about-name', name);
        setText('#profile-bio', currentUserProfile.about || '');
        setText('#about-bio', currentUserProfile.about || '');
        setText('#about-email', currentUserProfile.email || '');
        setText(
            '#about-birthdate',
            currentUserProfile.dateOfBirth
                ? new Date(currentUserProfile.dateOfBirth).toLocaleDateString('ru-RU')
                : '—'
        );

        if (currentUserProfile.avatarUrl) {
            applyAvatar(dom.$('#profile-avatar'), currentUserProfile.avatarUrl, name);
        }

        showAlert('#update-user-success', 'Изменения сохранены', 'success');
        toggleBusy(form, false);
    });
}

document.addEventListener(
    'DOMContentLoaded',
    async () => {
        if (!dom.$('#profile-posts')) return;
        try {
            bindAvatarPicker();
            await loadProfile();
            paging = { skip: 0, take: 10, busy: false, eof: false };
            await loadMorePosts();
        } catch (err) {
            ui.toast(err?.message || 'Ошибка загрузки профиля');
            bindAvatarPicker();
        }
    },
    { passive: true }
);
