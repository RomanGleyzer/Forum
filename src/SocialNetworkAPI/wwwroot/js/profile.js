let currentUserProfile = null;
let paging = { skip: 0, take: 10, busy: false, eof: false };

function getAuthToken() {
    return localStorage.getItem('token');
}

async function getCurrentUser() {
    if (currentUserProfile) return currentUserProfile;
    const token = getAuthToken();
    if (!token) throw new Error('Not authenticated');
    const response = await fetch('/api/users/me/profile', {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Ошибка загрузки профиля');
    currentUserProfile = await response.json();
    return currentUserProfile;
}

function fillProfile(user) {
    document.getElementById('profile-fullname').textContent =
        (user.firstName || '') + ' ' + (user.lastName || '');
    document.getElementById('profile-email').textContent = user.email || '';
    document.getElementById('profile-birthdate').textContent = user.dateOfBirth
        ? new Date(user.dateOfBirth).toLocaleDateString()
        : '';
    document.getElementById('profile-bio').textContent = user.about || '';
    if (user.avatarUrl)
        document.getElementById('profile-avatar').src = user.avatarUrl;
}

function fillEditProfileForm(user) {
    document.getElementById('firstName').value = user.firstName || '';
    document.getElementById('lastName').value = user.lastName || '';
    document.getElementById('email').value = user.email || '';
    document.getElementById('dateOfBirth').value = user.dateOfBirth
        ? user.dateOfBirth.slice(0, 10)
        : '';
    document.getElementById('about').value = user.about || '';
}

async function getUserPosts(userId, skip = 0, take = 10) {
    const token = getAuthToken();
    if (!token) throw new Error('Необходима авторизация!');
    const url = `/api/posts/${encodeURIComponent(userId)}/posts?skip=${skip}&take=${take}`;
    const resp = await fetch(url, { headers: { 'Authorization': `Bearer ${token}` } });
    if (!resp.ok) {
        const text = await safeReadJson(resp);
        const msg = (text && text.message) ? text.message : 'Ошибка загрузки постов';
        throw new Error(msg);
    }
    return resp.json();
}

function renderPosts(posts) {
    const container = document.getElementById('profile-posts');
    const noPosts = document.getElementById('no-posts');

    if (Array.isArray(posts) && posts.length > 0) {
        noPosts?.classList.add('d-none');
    } else if (paging.skip === 0) {
        noPosts?.classList.remove('d-none');
        return;
    }

    for (const p of posts) {
        const card = document.createElement('div');
        card.className = 'card mb-3 shadow-sm rounded-4';
        card.innerHTML = `
            <div class="card-body">
                <div class="d-flex align-items-center mb-2">
                    <div class="fw-semibold me-2">${escapeHtml(p.author?.firstName || '')} ${escapeHtml(p.author?.lastName || '')}</div>
                    <small class="text-muted">${formatDate(p.creationDate)}</small>
                </div>
                <div class="mb-2">${escapeHtml(p.content || '')}</div>
            </div>`;
        container.appendChild(card);
    }

    ensureLoadMoreButton();
}

function ensureLoadMoreButton() {
    const container = document.getElementById('profile-posts');
    let btn = document.getElementById('load-more-posts');
    if (paging.eof) {
        btn?.remove();
        return;
    }
    if (!btn) {
        btn = document.createElement('button');
        btn.id = 'load-more-posts';
        btn.className = 'btn btn-outline-secondary w-100 mb-3';
        btn.textContent = 'Показать ещё';
        btn.addEventListener('click', loadMorePosts);
        container.appendChild(btn);
    }
    btn.disabled = paging.busy;
}

async function loadMorePosts() {
    if (paging.busy || paging.eof) return;
    paging.busy = true;
    try {
        const posts = await getUserPosts(currentUserProfile.id, paging.skip, paging.take);
        renderPosts(posts);
        if (!Array.isArray(posts) || posts.length < paging.take) paging.eof = true;
        paging.skip += Array.isArray(posts) ? posts.length : 0;
    } catch (err) {
        console.error(err);
        showToast('Не удалось загрузить посты пользователя');
    } finally {
        paging.busy = false;
        ensureLoadMoreButton();
    }
}

function formatDate(iso) {
    try { return iso ? new Date(iso).toLocaleString() : ''; }
    catch { return ''; }
}

function escapeHtml(s) {
    return (s ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

async function safeReadJson(resp) {
    try { return await resp.json(); } catch { return null; }
}

function showToast(text) {
    console.warn(text);
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const user = await getCurrentUser();
        fillProfile(user);
        fillEditProfileForm(user);

        paging = { skip: 0, take: 10, busy: false, eof: false };
        const first = await getUserPosts(user.id, paging.skip, paging.take);
        renderPosts(first);
        if (!Array.isArray(first) || first.length < paging.take) paging.eof = true;
        paging.skip += Array.isArray(first) ? first.length : 0;
    } catch (err) {
        alert('Ошибка загрузки профиля: ' + err.message);
    }
});

document.getElementById('edit-profile-btn').addEventListener('click', () => {
    document.getElementById('update-user-error').classList.add('d-none');
    document.getElementById('update-user-success').classList.add('d-none');
    new bootstrap.Modal(document.getElementById('editProfileModal')).show();
});

document.getElementById('edit-profile-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const token = getAuthToken();
    if (!token) {
        alert('Необходима авторизация!');
        return;
    }
    document.getElementById('update-user-error').classList.add('d-none');
    document.getElementById('update-user-success').classList.add('d-none');

    const data = {
        firstName: document.getElementById('firstName').value.trim(),
        lastName: document.getElementById('lastName').value.trim(),
        email: document.getElementById('email').value.trim(),
        dateOfBirth: document.getElementById('dateOfBirth').value,
        about: document.getElementById('about').value.trim()
    };

    try {
        const response = await fetch('/api/users', {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            let errorText = 'Ошибка обновления профиля';
            try {
                const err = await response.json();
                errorText = err?.message || errorText;
            } catch { }
            document.getElementById('update-user-error').textContent = errorText;
            document.getElementById('update-user-error').classList.remove('d-none');
            return;
        }
        document.getElementById('update-user-success').textContent = 'Профиль успешно обновлен!';
        document.getElementById('update-user-success').classList.remove('d-none');
        currentUserProfile = { ...currentUserProfile, ...data };
        fillProfile(currentUserProfile);
        setTimeout(() => {
            bootstrap.Modal.getInstance(document.getElementById('editProfileModal')).hide();
        }, 1000);
    } catch (err) {
        document.getElementById('update-user-error').textContent = 'Ошибка соединения с сервером';
        document.getElementById('update-user-error').classList.remove('d-none');
    }
});
