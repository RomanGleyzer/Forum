import { http, dom, fmt, ui } from './shared.js';

const COMMENT_PAGE_SIZE = 5;

const renderCommentItem = (c) => {
    const li = dom.create('li', 'py-2 border-top');
    const name = fmt.escape(c?.author?.displayName ?? '');
    li.innerHTML = `
        <div class="d-flex">
            <div class="flex-grow-1">
                <div class="small">${fmt.escape(c?.content ?? '')}</div>
                <div class="text-muted small mt-1">${name} • ${fmt.dateTime(c?.creationDate)}</div>
            </div>
        </div>
    `;
    return li;
};

const setupComments = (rootEl, post) => {
    const wrap = rootEl.querySelector(`[data-comments-wrap="${post.id}"]`);
    if (!wrap) return;

    const list = wrap.querySelector('.comments-list');
    const form = wrap.querySelector('.comment-form');
    const textarea = wrap.querySelector('.comment-input');
    const moreBtn = wrap.querySelector('.comments-more');
    const emptyMsg = wrap.querySelector('.comments-empty');

    wrap.dataset.skip = post.featuredComment ? '1' : '0';
    wrap.dataset.loading = '0';

    if (post.featuredComment) {
        dom.show(emptyMsg, false);
        list.appendChild(renderCommentItem(post.featuredComment));
        dom.show(moreBtn, true);
    }

    form?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const content = textarea.value.trim();
        if (!content) {
            ui.toast('Введите текст комментария');
            return;
        }
        form.querySelectorAll('button,textarea').forEach(el => el.disabled = true);

        const { ok, json } = await http.post(`/api/posts/${encodeURIComponent(post.id)}/comments`, { content });

        if (!ok) {
            ui.toast(json?.message || 'Не удалось отправить комментарий');
            form.querySelectorAll('button,textarea').forEach(el => el.disabled = false);
            return;
        }

        const created = json ?? { content, author: { displayName: 'Вы' }, creationDate: new Date().toISOString() };
        dom.show(emptyMsg, false);
        list.prepend(renderCommentItem(created));

        const currentSkip = parseInt(wrap.dataset.skip || '0', 10);
        wrap.dataset.skip = String(currentSkip + 1);

        textarea.value = '';
        form.querySelectorAll('button,textarea').forEach(el => el.disabled = false);
    });

    moreBtn?.addEventListener('click', async () => {
        if (wrap.dataset.loading === '1') return;
        wrap.dataset.loading = '1';
        moreBtn.disabled = true;

        const skip = parseInt(wrap.dataset.skip || '0', 10);

        const { ok, json } = await http.get(`/api/posts/${encodeURIComponent(post.id)}/comments?skip=${skip}&take=${COMMENT_PAGE_SIZE}`);

        if (!ok) {
            ui.toast('Не удалось загрузить комментарии');
            moreBtn.disabled = false;
            wrap.dataset.loading = '0';
            return;
        }

        const items = Array.isArray(json) ? json : [];
        if (items.length === 0 && list.children.length === 0) {
            dom.show(emptyMsg, true);
        }

        for (const c of items) list.appendChild(renderCommentItem(c));

        wrap.dataset.skip = String(skip + items.length);

        if (items.length < COMMENT_PAGE_SIZE) {
            dom.show(moreBtn, false);
        } else {
            moreBtn.disabled = false;
        }

        wrap.dataset.loading = '0';
    });
};

export const renderPost = (post) => {
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

        <!-- Комментарии -->
        <section class="mt-3" aria-label="Комментарии к посту" data-comments-wrap="${post.id}">
            <div class="small text-muted mb-2">Комментарии</div>
            <ul class="list-unstyled mb-2 comments-list"></ul>

            <div class="text-muted small comments-empty ${post.featuredComment ? 'd-none' : ''}">Комментариев пока нет.</div>

            <div class="d-flex justify-content-end">
                <button type="button" class="btn btn-sm btn-outline-secondary comments-more ${post.featuredComment ? '' : 'd-none'}">
                    Показать ещё
                </button>
            </div>

            <form class="comment-form mt-3" autocomplete="off">
                <div class="input-group">
                    <textarea class="form-control comment-input" rows="1" placeholder="Напишите комментарий..." maxlength="500" aria-label="Текст комментария"></textarea>
                    <button class="btn btn-primary" type="submit">Отправить</button>
                </div>
            </form>
        </section>
      </div>
    </div>`;

    setupComments(li, post);
    return li;
};

window.renderPost = renderPost;
