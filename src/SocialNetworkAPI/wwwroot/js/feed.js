import { dom, http, ui } from './shared.js';
import { renderPost } from './post.js';

const postsList = document.getElementById('posts');
let endReached = false;
let loading = false;
let inflightCtrl = null;
const take = 10;

let cursorCreatedAt = null;
let cursorId = null;
let skip = 0;

let spinner = null;
let sentinel = null;

function hideSkeletons() {
  dom.$$('#posts [data-skeleton="post"]').forEach(el => el.remove());
}

function pickItems(json) {
  if (Array.isArray(json)) return json;
  if (Array.isArray(json?.items)) return json.items;
  if (Array.isArray(json?.data)) return json.data;
  if (Array.isArray(json?.posts)) return json.posts;
  return [];
}

function renderPosts(posts) {
  const frag = dom.fragment();
  for (const post of posts) frag.append(renderPost(post));
  postsList.append(frag);
}

function ensureEndOfFeed() {
  let endMsg = dom.$('#end-of-feed');
  if (!endMsg) {
    endMsg = dom.create('div', 'text-center text-muted py-3');
    endMsg.id = 'end-of-feed';
    endMsg.textContent = 'Больше нет постов.';
    postsList?.after(endMsg);
  }
}

async function loadPosts() {
  if (endReached || loading) return;
  loading = true;

  inflightCtrl?.abort();
  inflightCtrl = new AbortController();

  dom.show(spinner, true);

  const params = new URLSearchParams();
  params.append('take', String(take));

  if (cursorCreatedAt && cursorId) {
    params.append('cursorCreatedAt', cursorCreatedAt);
    params.append('cursorId', cursorId);
  } else {
    params.append('skip', String(skip));
  }

  const { ok, json } = await http.get(`/api/posts?${params.toString()}`, { signal: inflightCtrl.signal });
  dom.hide(spinner);

  if (!ok) {
    ui.toast(json?.message || 'Не удалось загрузить ленту');
    loading = false;
    return;
  }

  const items = pickItems(json);
  if (items.length === 0) {
    endReached = true;
    ensureEndOfFeed();
    loading = false;
    return;
  }

  hideSkeletons();
  renderPosts(items);

  const last = items.at(-1);
  if (last?.creationDate && last?.id != null) {
    cursorCreatedAt = last.creationDate;
    cursorId = last.id;
  }
  skip += items.length;

  loading = false;
}

function setupObserver() {
  const io = new IntersectionObserver(entries => {
    if (entries.some(e => e.isIntersecting)) loadPosts();
  }, { rootMargin: '300px' });
  io.observe(sentinel);
}

function bindComposer() {
  const form = document.getElementById('post-form');
  if (!form) return;

  const textarea = form.querySelector('textarea') || document.getElementById('post-content');
  const submitBtn = form.querySelector('button[type="submit"]');

  const setBusy = (v) => {
    form.querySelectorAll('input,textarea,button').forEach(el => el.disabled = !!v);
    if (submitBtn) submitBtn.ariaBusy = String(!!v);
    form.setAttribute('aria-busy', String(!!v));
  };

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const content = (textarea?.value || '').trim();
    if (!content) return;

    try {
      setBusy(true);

      const { ok, json } = await http.post('/api/posts', { content });

      if (!ok) {
        ui.toast(json?.message || 'Не удалось опубликовать пост');
        return;
      }

      if (textarea) {
        textarea.value = '';
        textarea.style.height = '';
      }

      if (postsList) {
        const node = renderPost(json);
        postsList.prepend(node);
        skip += 1;
      }
    } catch (err) {
      ui.toast('Не удалось опубликовать пост');
    } finally {
      setBusy(false);
    }
  }, { passive: false });
}

document.addEventListener('DOMContentLoaded', () => {
  bindComposer();

  if (!postsList) return;

  sentinel = dom.create('div', 'ui-infinite-sentinel');
  sentinel.id = 'feed-sentinel';
  postsList.after(sentinel);

  spinner = ui.ensureSpinnerBelow(postsList, 'feed-spinner');
  dom.hide(spinner);

  setupObserver();
  loadPosts();
}, { passive: true });
