document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('post-form');
    const feed = document.getElementById('posts');
    if (!form || !feed) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const textarea = document.getElementById('post-content');
        const content = textarea.value.trim();

        if (!content) return alert('Введите текст поста');

        const noPostsMsg = document.getElementById('no-posts');
        if (noPostsMsg) noPostsMsg.style.display = 'none';

        const tempPost = document.createElement('li');
        tempPost.textContent = content + ' (отправка...)';
        feed.prepend(tempPost);

        const token = localStorage.getItem('token');
        try {
            const resp = await fetch('/api/posts', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + token
                },
                body: JSON.stringify({ content })
            });

            if (!resp.ok) {
                tempPost.remove();
                if (!feed.querySelector('li')) {
                    if (noPostsMsg) noPostsMsg.style.display = '';
                }
                const err = await safeReadJson(resp);
                alert((err && err.message) || 'Ошибка публикации');
                return;
            }

            let created = null;
            try { created = await resp.json(); } catch { /* тело может быть пустым */ }

            let fullPost = null;

            const createdId = created?.id;

            const location = resp.headers.get('Location');

            if (createdId) {
                fullPost = await fetchPostById(createdId, token);
            } else if (location) {
                fullPost = await fetchPostByUrl(location, token);
            } else if (created && created.content && created.creationDate) {
                fullPost = created;
            }

            tempPost.remove();

            if (!fullPost) {
                fullPost = {
                    author: created?.author ?? null,
                    creationDate: new Date().toISOString(),
                    content: content
                };
            }

            const li = renderSinglePost(fullPost);
            feed.prepend(li);
            textarea.value = '';
        } catch {
            tempPost.remove();
            if (!feed.querySelector('li')) {
                if (noPostsMsg) noPostsMsg.style.display = '';
            }
            alert('Сетевая ошибка!');
        }
    });
});

async function fetchPostById(id, token) {
    const resp = await fetch(`/api/posts/${encodeURIComponent(id)}`, {
        headers: { 'Authorization': 'Bearer ' + token }
    });
    if (!resp.ok) return null;
    return resp.json();
}

async function fetchPostByUrl(url, token) {
    const resp = await fetch(url, {
        headers: { 'Authorization': 'Bearer ' + token }
    });
    if (!resp.ok) return null;
    return resp.json();
}

function renderSinglePost(post) {
    const li = document.createElement('li');
    li.className = 'mb-4';

    const avatar = (post.author && post.author.avatarUrl) ? post.author.avatarUrl : 'images/user-default.png';
    const name = [
        (post.author && post.author.firstName) ? post.author.firstName : '',
        (post.author && post.author.lastName) ? post.author.lastName : ''
    ].join(' ').trim();

    li.innerHTML = `
        <div class="card post-card shadow-sm">
            <div class="card-body">
                <div class="d-flex align-items-center mb-2">
                    <img src="${escapeHtml(avatar)}" class="rounded-circle me-2" width="40" height="40" alt="avatar">
                    <div>
                        <div class="fw-bold">${escapeHtml(name)}</div>
                        <small class="text-muted">${formatDateSafe(post.creationDate)}</small>
                    </div>
                </div>
                <div class="mb-2">${escapeHtml(post.content ?? '')}</div>
                ${post.featuredComment ? renderFeaturedComment(post.featuredComment) : ''}
            </div>
        </div>
    `;
    return li;
}

function formatDateSafe(iso) {
    if (!iso) return 'только что';
    try {
        const dt = new Date(iso);
        const s = dt.toLocaleString();
        return s === 'Invalid Date' ? 'только что' : s;
    } catch {
        return 'только что';
    }
}