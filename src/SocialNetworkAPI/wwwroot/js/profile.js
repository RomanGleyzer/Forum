import { http, dom, fmt, ui } from './shared.js';

let currentUserProfile = null;
let paging = { skip: 0, take: 10, busy: false, eof: false };

const AVATAR = {
    maxBytes: 2 * 1024 * 1024,
    types: new Set(['image/jpeg', 'image/png', 'image/webp', 'image/avif']),
};

const $avatarImg = () => dom.$('#profile-avatar');
const $avatarUpload = () => dom.$('#avatar-upload');
const $avatarChangeBtn = () => dom.$('#change-avatar-btn');

let uploadBusy = false;
let previewUrlToRevoke = null;

const validateFile = (file) => {
    if (!file) { ui.toast('Файл не выбран'); return false; }
    if (!AVATAR.types.has(file.type)) { ui.toast('Допустимы JPG, PNG, WEBP, AVIF'); return false; }
    if (file.size > AVATAR.maxBytes) { ui.toast('Файл слишком большой (макс. 2 МБ)'); return false; }
    return true;
};

const withBust = (url) => url + (url.includes('?') ? '&' : '?') + 't=' + Date.now();

const previewLocally = (file) => {
    if (previewUrlToRevoke) {
        URL.revokeObjectURL(previewUrlToRevoke);
        previewUrlToRevoke = null;
    }
    const url = URL.createObjectURL(file);
    $avatarImg().src = url;
    previewUrlToRevoke = url;
};

const uploadAvatar = async (file) => {
    if (uploadBusy) return;
    if (!validateFile(file)) return;

    const oldSrc = $avatarImg().src;
    previewLocally(file);

    uploadBusy = true;
    const ctrl = new AbortController();

    try {
        const form = new FormData();
        form.append('file', file, file.name || 'avatar');

        const { ok, json } = await http.upload('/api/users/me/avatar', form, { signal: ctrl.signal });

        if (!ok) throw new Error(json?.message || 'Ошибка загрузки аватара');

        const newUrl = json?.url ? withBust(json.url) : null;
        if (newUrl) $avatarImg().src = newUrl;

        ui.toast('Аватар обновлён!', 'success');
    } catch (err) {
        $avatarImg().src = oldSrc;
        ui.toast(err.message || 'Не удалось обновить аватар');
    } finally {
        uploadBusy = false;
        if (previewUrlToRevoke) {
            URL.revokeObjectURL(previewUrlToRevoke);
            previewUrlToRevoke = null;
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    // клик по кнопке с камерой
    $avatarChangeBtn()?.addEventListener('click', () => $avatarUpload()?.click(), { passive: true });

    // выбор файла
    $avatarUpload()?.addEventListener('change', (e) => {
        const file = e.target.files?.[0];
        if (file) uploadAvatar(file);
        e.target.value = '';
    });
});

const getCurrentUser = async () => {
    if (currentUserProfile) return currentUserProfile;
    const { ok, json } = await http.get('/api/users/me/profile');
    if (!ok) throw new Error('Ошибка загрузки профиля');
    currentUserProfile = json;
    return json;
};

const fillProfile = (u) => {
    dom.$('#profile-fullname').textContent = `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim();
    dom.$('#profile-email').textContent = u.email ?? '';
    dom.$('#profile-birthdate').textContent = u.dateOfBirth ? new Date(u.dateOfBirth).toLocaleDateString() : '';
    dom.$('#profile-bio').textContent = u.about ?? '';
    if (u.avatarUrl) dom.$('#profile-avatar').src = u.avatarUrl;
};

const fillEditProfileForm = (u) => {
    dom.$('#firstName').value = u.firstName ?? '';
    dom.$('#lastName').value = u.lastName ?? '';
    dom.$('#email').value = u.email ?? '';
    dom.$('#dateOfBirth').value = u.dateOfBirth ? u.dateOfBirth.slice(0, 10) : '';
    dom.$('#about').value = u.about ?? '';
};

const getUserPosts = async (userId, skip = 0, take = 10) => {

    const { ok, json } = await http.get(
        `/api/users/${encodeURIComponent(userId)}/posts?skip=${skip}&take=${take}`
    );

    if (!ok) throw new Error(json?.message || 'Ошибка загрузки постов');
    return Array.isArray(json) ? json : [];
};

const renderPosts = (posts) => {
    const container = dom.$('#profile-posts');
    const noPosts = dom.$('#no-posts');

    if (posts.length) dom.hide(noPosts);
    else if (paging.skip === 0) { dom.show(noPosts, true); return; }

    const frag = dom.fragment();
    for (const p of posts) {
        const avatar = p.author?.avatarUrl
            ? `<img src="${fmt.escape(p.author.avatarUrl)}" alt="avatar" 
                   class="rounded-circle me-2" style="width:32px;height:32px;object-fit:cover;">`
            : `<div class="rounded-circle bg-secondary me-2" 
                   style="width:32px;height:32px;"></div>`;

        const card = dom.create('div', 'card mb-3 shadow-sm rounded-4');
        card.innerHTML = `
      <div class="card-body">
        <div class="d-flex align-items-center mb-2">
          ${avatar}
          <div class="fw-semibold me-2">${fmt.escape(p.author?.firstName || '')} ${fmt.escape(p.author?.lastName || '')}</div>
          <small class="text-muted">${fmt.dateTime(p.creationDate)}</small>
        </div>
        <div class="mb-2">${fmt.escape(p.content || '')}</div>
      </div>`;
        frag.append(card);
    }
    container.append(frag);
    ensureLoadMoreButton();
};

function ensureLoadMoreButton() {
    const container = dom.$('#profile-posts');
    let btn = dom.$('#load-more-posts');

    if (paging.eof) { btn?.remove(); return; }

    if (!btn) {
        btn = dom.create('button', 'btn btn-outline-secondary w-100 mb-3');
        btn.id = 'load-more-posts';
        btn.textContent = 'Показать ещё';
        btn.addEventListener('click', loadMorePosts, { passive: true });
    }

    btn.disabled = paging.busy;
    container.append(btn);
}

async function loadMorePosts() {
    if (paging.busy || paging.eof) return;
    paging.busy = true;
    try {
        const posts = await getUserPosts(currentUserProfile.id, paging.skip, paging.take);
        renderPosts(posts);
        if (posts.length < paging.take) paging.eof = true;
        paging.skip += posts.length;
    } catch (err) {
        console.error(err);
        ui.toast('Не удалось загрузить посты пользователя');
    } finally {
        paging.busy = false;
        ensureLoadMoreButton();
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const user = await getCurrentUser();
        fillProfile(user);
        fillEditProfileForm(user);

        paging = { skip: 0, take: 10, busy: false, eof: false };
        const first = await getUserPosts(user.id, paging.skip, paging.take);
        renderPosts(first);
        if (first.length < paging.take) paging.eof = true;
        paging.skip += first.length;
    } catch (err) {
        ui.toast('Ошибка загрузки профиля: ' + err.message);
    }
});

dom.$('#edit-profile-btn')?.addEventListener('click', () => {
    dom.$('#update-user-error')?.classList.add('d-none');
    dom.$('#update-user-success')?.classList.add('d-none');
    new bootstrap.Modal(dom.$('#editProfileModal')).show();
});

dom.$('#edit-profile-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    dom.$('#update-user-error')?.classList.add('d-none');
    dom.$('#update-user-success')?.classList.add('d-none');

    const data = {
        firstName: dom.$('#firstName').value.trim(),
        lastName: dom.$('#lastName').value.trim(),
        email: dom.$('#email').value.trim(),
        dateOfBirth: dom.$('#dateOfBirth').value,
        about: dom.$('#about').value.trim(),
    };

    const { ok, json } = await http.put('/api/users', data);
    if (!ok) {
        dom.$('#update-user-error').textContent = json?.message || 'Ошибка обновления профиля';
        dom.$('#update-user-error').classList.remove('d-none');
        return;
    }

    dom.$('#update-user-success').textContent = 'Профиль успешно обновлен!';
    dom.$('#update-user-success').classList.remove('d-none');
    currentUserProfile = { ...currentUserProfile, ...data };
    fillProfile(currentUserProfile);
    setTimeout(() => bootstrap.Modal.getInstance(dom.$('#editProfileModal')).hide(), 1000);
});
