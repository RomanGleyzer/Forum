import { fmt, http, ui } from './shared.js';

const AVATAR_FALLBACK = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='40' height='40'%3E%3Cdefs/%3E%3Ccircle cx='20' cy='20' r='20' fill='%23ddd'/%3E%3Ctext x='50%25' y='58%25' font-size='16' text-anchor='middle' fill='%23666'%3F%3E%3F%3C/text%3E%3C/svg%3E";

export function renderPost(post) {
  const li = document.createElement('li');
  li.className = 'mb-4';

  const avatar = post?.author?.avatarUrl || AVATAR_FALLBACK;
  const name = `${post?.author?.firstName ?? ''} ${post?.author?.lastName ?? ''}`.trim() || 'Пользователь';
  const content = post?.content ?? '';
  const createdAt = post?.creationDate;

  li.innerHTML = `
    <article class="card post-card shadow-sm">
      <div class="card-body">
        <header class="d-flex align-items-center mb-2">
          <img src="${fmt.escape(avatar)}" class="rounded-circle me-2 ui-avatar" width="40" height="40" alt="avatar">
          <div>
            <div class="fw-bold">${fmt.escape(name)}</div>
            <small class="text-muted">${fmt.dateTime(createdAt)}</small>
          </div>
        </header>

        <div class="mb-2">${fmt.escape(content)}</div>

        <section class="mt-3" aria-label="Комментарии к посту" data-comments-wrap="${post.id}">
          <div class="small text-muted mb-2">Комментарии</div>
          <ul class="list-unstyled mb-2 comments-list"></ul>
          <div class="text-muted small comments-empty ${post?.featuredComment ? 'd-none' : ''}">
            Комментариев пока нет.
          </div>
          <div class="d-flex justify-content-end">
            <form class="d-flex gap-2 align-items-center comment-form" data-post-id="${post.id}">
              <label class="visually-hidden" for="comment-${post.id}">Комментарий</label>
              <input id="comment-${post.id}" class="form-control form-control-sm" type="text"
                     placeholder="Оставить комментарий…" maxlength="500" />
              <button type="submit" class="btn btn-primary btn-sm">Отправить</button>
            </form>
          </div>
        </section>
      </div>
    </article>
  `;

  li.querySelectorAll('img').forEach(img => {
    img.addEventListener('error', () => { img.src = AVATAR_FALLBACK; }, { once: true });
  });

  const list = li.querySelector('.comments-list');
  const empty = li.querySelector('.comments-empty');

  if (post?.featuredComment) {
    list?.appendChild(renderComment(post.featuredComment));
    empty?.classList.add('d-none');
  }

  const form = li.querySelector('.comment-form');
  form?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const input = form.querySelector('input');
    const text = input?.value?.trim();
    if (!text) return;

    const postId = Number(form.getAttribute('data-post-id'));
    const { ok, json } = await http.post(`/api/posts/${postId}/comments`, { content: text });
    if (ok) {
      input.value = '';
      list?.appendChild(renderComment(json));
      empty?.classList.add('d-none');
    } else {
      ui.toast(json?.message || 'Не удалось отправить комментарий');
    }
  });

  return li;
}

function renderComment(c) {
  const li = document.createElement('li');
  li.className = 'd-flex align-items-start gap-2 py-1';
  const name = `${c?.author?.firstName ?? ''} ${c?.author?.lastName ?? ''}`.trim() || 'Пользователь';

  const avatar = c?.author?.avatarUrl || AVATAR_FALLBACK;

  li.innerHTML = `
    <img src="${fmt.escape(avatar)}" class="rounded-circle mt-1 ui-avatar" width="28" height="28" alt="avatar">
    <div>
      <div class="small">
        <span class="fw-semibold">${fmt.escape(name)}</span>
        <span class="text-muted">${fmt.dateTime(c?.creationDate)}</span>
      </div>
      <div class="small">${fmt.escape(c?.content ?? '')}</div>
    </div>
  `;

  const img = li.querySelector('img');
  img?.addEventListener('error', () => { img.src = AVATAR_FALLBACK; }, { once: true });

  return li;
}
