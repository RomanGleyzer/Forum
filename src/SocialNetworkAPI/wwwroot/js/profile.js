// profile.js
import { http, dom, fmt, ui } from './shared.js';
import { renderPost } from './post.js';

let currentUserProfile = null;
let paging = { skip: 0, take: 10, busy: false, eof: false };

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
        // используем общий рендер поста с комментариями
        const li = renderPost(p);
        frag.append(li);
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
        const { ok, json } = await http.get('/api/users/me/profile');
        if (!ok) throw new Error('Ошибка загрузки профиля');
        currentUserProfile = json;

        dom.$('#profile-fullname').textContent = `${json.firstName ?? ''} ${json.lastName ?? ''}`.trim();
        dom.$('#profile-email').textContent = json.email ?? '';
        dom.$('#profile-birthdate').textContent = json.dateOfBirth ? new Date(json.dateOfBirth).toLocaleDateString() : '';
        dom.$('#profile-bio').textContent = json.about ?? '';
        if (json.avatarUrl) dom.$('#profile-avatar').src = json.avatarUrl;

        paging = { skip: 0, take: 10, busy: false, eof: false };
        const first = await getUserPosts(json.id, paging.skip, paging.take);
        renderPosts(first);
        if (first.length < paging.take) paging.eof = true;
        paging.skip += first.length;
    } catch (err) {
        ui.toast('Ошибка загрузки профиля: ' + err.message);
    }
});
