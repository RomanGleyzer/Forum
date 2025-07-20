document.getElementById('post-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const textarea = document.getElementById('post-content');
    const content = textarea.value.trim();

    if (!content) return alert('Введите текст поста');

    const feed = document.getElementById('feed');
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
            const err = await resp.json();
            alert(err.message || 'Ошибка публикации');
        }
    } catch {
        feed.removeChild(tempPost);
        alert('Сетевая ошибка!');
    }
    textarea.value = '';
});
