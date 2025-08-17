const REDIRECT_TO_LOGIN = () => { location.href = 'login.html'; };

export const storage = {
    getToken: () => localStorage.getItem('token'),
    setToken: (t) => localStorage.setItem('token', t),
    clearToken: () => localStorage.removeItem('token'),
};

const buildHeaders = (extra = {}) => {
    const token = storage.getToken();
    const h = { Accept: 'application/json', ...extra };
    if (token) h.Authorization = `Bearer ${token}`;
    return h;
};

const safeJson = async (resp) => {
    const text = await resp.text().catch(() => '');
    if (!text) return null;
    try { return JSON.parse(text); } catch { return null; }
};

const handleAuth = (resp) => {
    if (resp.status === 401) {
        storage.clearToken();
        REDIRECT_TO_LOGIN();
        return false;
    }
    return true;
};

const request = async (url, options = {}, { signal } = {}) => {
    const ctrl = new AbortController();
    const compositeSignal = signal
        ? (AbortSignal.any ? AbortSignal.any([signal, ctrl.signal]) : signal)
        : ctrl.signal;

    let resp;
    try {
        resp = await fetch(url, { ...options, signal: compositeSignal });
    } catch (err) {
        if (err?.name === 'AbortError') {
            return { ok: false, status: 0, json: null, resp: undefined, aborted: true, error: err };
        }
        return { ok: false, status: 0, json: null, resp: undefined, aborted: false, error: err };
    }

    if (!handleAuth(resp)) return { ok: false, status: 401, json: null, resp };
    const json = await safeJson(resp);
    return { ok: resp.ok, status: resp.status, json, resp };
};


export const http = {
    get: (url, opts = {}) =>
        request(url, { headers: buildHeaders(), ...opts }, opts),
    post: (url, body, opts = {}) =>
        request(url, {
            method: 'POST',
            headers: buildHeaders({ 'Content-Type': 'application/json' }),
            body: JSON.stringify(body),
            ...opts,
        }, opts),
    put: (url, body, opts = {}) =>
        request(url, {
            method: 'PUT',
            headers: buildHeaders({ 'Content-Type': 'application/json' }),
            body: JSON.stringify(body),
            ...opts,
        }, opts),
};

export const dom = {
    $: (s, r = document) => r.querySelector(s),
    $$: (s, r = document) => Array.from(r.querySelectorAll(s)),
    create: (tag, cls) => {
        const el = document.createElement(tag);
        if (cls) el.className = cls;
        return el;
    },
    fragment: () => document.createDocumentFragment(),
    show: (el, show = true) => { if (el) el.classList.toggle('d-none', !show); },
    hide: (el) => { if (el) el.classList.add('d-none'); },
};

export const fmt = {
    escape: (s) => String(s ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;'),
    dateTime: (iso, locale = 'ru-RU') => {
        if (!iso) return 'только что';
        const d = new Date(iso);
        return Number.isNaN(d.getTime())
            ? 'только что'
            : d.toLocaleString(locale, { dateStyle: 'short', timeStyle: 'short' });
    },
};

export const ui = {
    toast: (text, type = 'danger') => {
        console[type === 'danger' ? 'error' : 'log'](text);
        alert(text);
    },
    ensureSpinnerBelow: (anchor, id = 'spinner') => {
        let sp = document.getElementById(id);
        if (!sp) {
            sp = dom.create('div', 'text-center py-3');
            sp.id = id;
            sp.innerHTML = `<div class="spinner-border" role="status"></div>`;
            anchor.after(sp);
        }
        return sp;
    },
};
