import { http, dom, fmt, ui } from './shared.js';

let cursorCreatedAt = null;
let cursorId = null;
let endReached = false;
let inflightCtrl = null;
let loading = false;

const take = 10;
const postsList = dom.$('#posts');
const noPostsMsg = dom.$('#no-posts');
const sentinel = dom.create('div');
sentinel.id = 'feed-sentinel';
postsList?.after(sentinel);

const spinner = ui.ensureSpinnerBelow(postsList, 'feed-spinner');
dom.hide(spinner);

const observe = () => {
    const io = new IntersectionObserver(entries => {
        if (entries.some(e => e.isIntersecting)) loadPosts();
    }, { rootMargin: '300px' });
    io.observe(sentinel);
};

document.addEventListener('DOMContentLoaded', () => {
    if (!postsList) return;
    loadPosts();
    observe();
});

async function loadPosts() {
    if (endReached || loading) return;
    loading = true;

    // Отменяем предыдущий запрос (если был)
    inflightCtrl?.abort();
    inflightCtrl = new AbortController();

    dom.show(spinner, true);

    const params = new URLSearchParams();
    if (cursorCreatedAt && cursorId) {
        params.append('cursorCreatedAt', cursorCreatedAt);
        params.append('cursorId', cursorId);
    }
    params.append('take', String(take));

    const res = await http.get(`/api/posts?${params}`, { signal: inflightCtrl.signal });

    dom.show(spinner, false);
    loading = false;

    if (res.aborted) return;
    if (!res.ok) { ui.toast('Ошибка при загрузке ленты'); return; }

    const posts = Array.isArray(res.json) ? res.json : [];
    if (!posts.length) {
        endReached = true;
        ensureEndOfFeed();
        if (!cursorCreatedAt || !cursorId) dom.show(noPostsMsg, true);
        return;
    }

    dom.show(noPostsMsg, false);
    renderPosts(posts);

    // обновляем курсор по последнему посту на странице
    const last = posts[posts.length - 1];
    cursorCreatedAt = last?.creationDate ?? cursorCreatedAt;
    cursorId = last?.id ?? cursorId;
}

function renderPosts(posts) {
    const frag = dom.fragment();
    for (const post of posts) frag.append(renderPost(post));
    postsList.append(frag);
}

function renderPost(post) {
    const li = dom.create('li', 'mb-4');
    const avatar = post.author?.avatarUrl || 'images/user-default.png';
    const name = `${post.author?.firstName ?? ''} ${post.author?.lastName ?? ''}`.trim();

    li.innerHTML = `
    <div class="card post-card shadow-sm">
      <div class="card-body">
        <div class="d-flex align-items-center mb-2">
          <img src="${fmt.escape(avatar)}" class="rounded-circle me-2" width="40" height="40" alt="avatar">
          <div>
            <div class="fw-bold">${fmt.escape(name)}</div>
            <small class="text-muted">${fmt.dateTime(post.creationDate)}</small>
          </div>
        </div>
        <div class="mb-2">${fmt.escape(post.content ?? '')}</div>
        ${post.featuredComment ? renderFeaturedComment(post.featuredComment) : ''}
      </div>
    </div>`;
    return li;
}

function renderFeaturedComment(comment) {
    return `
    <div class="mt-3 p-2 rounded bg-light">
      <div class="small text-muted mb-1">Комментарий:</div>
      <div>${fmt.escape(comment.content)}</div>
      <div class="text-end small text-muted mt-1">
        ${fmt.escape(`${comment.author?.firstName ?? ''} ${comment.author?.lastName ?? ''}`.trim())}
      </div>
    </div>`;
}

function ensureEndOfFeed() {
    let endMsg = dom.$('#end-of-feed');
    if (!endMsg) {
        endMsg = dom.create('div', 'text-center text-muted py-3');
        endMsg.id = 'end-of-feed';
        endMsg.textContent = 'Больше нет постов.';
        postsList.after(endMsg);
    }
    dom.show(endMsg, true);
}
