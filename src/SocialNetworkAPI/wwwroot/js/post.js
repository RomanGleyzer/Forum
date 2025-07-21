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
                tempPost.textContent = post.content + ' (опубликовано)';
            } else {
                feed.removeChild(tempPost);
                if (!feed.querySelector('li')) {
                    if (noPostsMsg) noPostsMsg.style.display = '';
                }
                const err = await resp.json();
                alert(err.message || 'Ошибка публикации');
            }
        } catch {
            feed.removeChild(tempPost);
            if (!feed.querySelector('li')) {
                if (noPostsMsg) noPostsMsg.style.display = '';
            }
            alert('Сетевая ошибка!');
        }
        textarea.value = '';
    });
});
