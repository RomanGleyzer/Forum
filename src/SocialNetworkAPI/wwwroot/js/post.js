import { http, dom, fmt, ui } from './shared.js';

const renderPost = (post) => {
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
        ${post.featuredComment ? `
          <div class="mt-3 p-2 rounded bg-light">
            <div class="small text-muted mb-1">Комментарий:</div>
            <div>${fmt.escape(post.featuredComment.content)}</div>
            <div class="text-end small text-muted mt-1">${fmt.escape(post.featuredComment.author?.displayName ?? '')}</div>
          </div>` : ''}
      </div>
    </div>`;
    return li;
};

document.addEventListener('DOMContentLoaded', () => {
    const form = dom.$('#post-form');
    const feed = dom.$('#posts');
    const noPostsMsg = dom.$('#no-posts');
    if (!form || !feed) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const textarea = dom.$('#post-content');
        const content = textarea?.value.trim();
        if (!content) { ui.toast('Введите текст поста'); return; }

        dom.show(noPostsMsg, false);
        const temp = dom.create('li'); temp.textContent = `${content} (отправка...)`;
        feed.prepend(temp);
        dom.$$('button,textarea', form).forEach(el => el.disabled = true);

        const { ok, json, resp } = await http.post('/api/posts', { content });

        if (!ok) {
            temp.remove();
            if (!feed.querySelector('li')) dom.show(noPostsMsg, true);
            ui.toast(json?.message || 'Ошибка публикации');
            dom.$$('button,textarea', form).forEach(el => el.disabled = false);
            return;
        }

        let full = json;
        const createdId = json?.id;
        const locationHdr = resp.headers.get('Location');

        if (createdId) {
            const r = await http.get(`/api/posts/${encodeURIComponent(createdId)}`);
            if (r.ok) full = r.json;
        } else if (locationHdr) {
            const r = await http.get(locationHdr);
            if (r.ok) full = r.json;
        }

        temp.remove();
        feed.prepend(renderPost(full ?? {
            author: json?.author ?? null,
            creationDate: new Date().toISOString(),
            content
        }));
        textarea.value = '';
        dom.$$('button,textarea', form).forEach(el => el.disabled = false);
    });
});
