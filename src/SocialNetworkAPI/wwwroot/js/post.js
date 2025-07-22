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

            if (resp.ok) {
                const post = await resp.json();
                tempPost.remove();
                const li = renderSinglePost(post);
                feed.prepend(li);
                textarea.value = '';
            } else {
                tempPost.remove();
                if (!feed.querySelector('li')) {
                    if (noPostsMsg) noPostsMsg.style.display = '';
                }
                const err = await resp.json();
                alert(err.message || 'Ошибка публикации');
            }
        } catch {
            tempPost.remove();
            if (!feed.querySelector('li')) {
                if (noPostsMsg) noPostsMsg.style.display = '';
            }
            alert('Сетевая ошибка!');
        }
    });
});

function renderSinglePost(post) {
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
    return li;
}