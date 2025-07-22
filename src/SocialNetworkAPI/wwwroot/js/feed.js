let cursor = null;
let loading = false;
let endReached = false;

const take = 10;

const postsList = document.getElementById('posts');
const noPostsMsg = document.getElementById('no-posts');

document.addEventListener('DOMContentLoaded', () => {
    loadPosts();
});

window.addEventListener('scroll', () => {
    if (loading || endReached) return;

    if (window.innerHeight + window.scrollY >= document.body.offsetHeight - 300) {
        loadPosts();
    }
});

async function loadPosts() {
    if (loading || endReached) return;
    loading = true;
    showLoadingSpinner(true);

    const params = new URLSearchParams();
    if (cursor) params.append('cursor', cursor);
    params.append('take', take);

    const token = localStorage.getItem('token');

    try {
        const resp = await fetch(`/api/posts?${params.toString()}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });
        if (!resp.ok) throw new Error('Ошибка загрузки');

        const posts = await resp.json();

        if (!posts.length) {
            endReached = true;
            showEndOfFeed();
            if (!cursor) showNoPosts();
        } else {
            renderPosts(posts);
            cursor = posts[posts.length - 1].creationDate;
        }
    } catch (e) {
        showError('Ошибка при загрузке ленты');
    } finally {
        showLoadingSpinner(false);
        loading = false;
    }
}

function renderPosts(posts) {
    noPostsMsg.style.display = 'none';

    for (const post of posts) {
        const li = document.createElement('li');
        li.className = 'mb-4';

        li.innerHTML = `
            <div class="card post-card shadow-sm">
                <div class="card-body">
                    <div class="d-flex align-items-center mb-2">
                        <img src="${escapeHtml(post.author?.avatarUrl ?? 'images/user-default.png')}" class="rounded-circle me-2" width="40" height="40" alt="avatar">
                        <div>
                            <div class="fw-bold">
                                ${escapeHtml((post.author?.firstName ?? '') + ' ' + (post.author?.lastName ?? ''))}
                            </div>
                            <small class="text-muted">${formatDate(post.creationDate)}</small>
                        </div>
                    </div>
                    <div class="mb-2">${escapeHtml(post.content)}</div>
                    ${post.featuredComment ? renderFeaturedComment(post.featuredComment) : ''}
                </div>
            </div>
        `;

        postsList.appendChild(li);
    }
}

function renderFeaturedComment(comment) {
    return `
        <div class="mt-3 p-2 rounded bg-light">
            <div class="small text-muted mb-1">Комментарий:</div>
            <div>${escapeHtml(comment.content)}</div>
            <div class="text-end small text-muted mt-1">${escapeHtml(comment.author?.displayName ?? '')}</div>
        </div>
    `;
}

function showLoadingSpinner(show) {
    let spinner = document.getElementById('feed-spinner');
    if (!spinner) {
        spinner = document.createElement('div');
        spinner.id = 'feed-spinner';
        spinner.className = 'text-center py-3';
        spinner.innerHTML = `<div class="spinner-border" role="status"></div>`;
        postsList.after(spinner);
    }
    spinner.style.display = show ? 'block' : 'none';
}

function showEndOfFeed() {
    let endMsg = document.getElementById('end-of-feed');
    if (!endMsg) {
        endMsg = document.createElement('div');
        endMsg.id = 'end-of-feed';
        endMsg.className = 'text-center text-muted py-3';
        endMsg.textContent = 'Больше нет постов.';
        postsList.after(endMsg);
    }
    endMsg.style.display = 'block';
}

function showNoPosts() {
    noPostsMsg.style.display = 'block';
}

function showError(msg) {
    alert(msg);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatDate(isoString) {
    try {
        const date = new Date(isoString);
        return date.toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' });
    } catch {
        return isoString;
    }
}
