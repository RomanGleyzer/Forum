export const storage = {
    getToken: () => localStorage.getItem('token'),
    setToken: (t) => localStorage.setItem('token', t),
    clearToken: () => localStorage.removeItem('token'),
};

const buildHeaders = (extra = {}) => {
    const token = storage.getToken();
    const h = {Accept: 'application/json', 'Content-Type': 'application/json', ...extra};
    if (token) h['Authorization'] = `Bearer ${token}`;
    return h;
};

const safeJson = async (res) => {
    try {
        return await res.json();
    } catch {
        return null;
    }
};

export const http = {
    get: async (url, {signal} = {}) => {
        const res = await fetch(url, {method: 'GET', headers: buildHeaders(), signal});
        return {ok: res.ok, status: res.status, json: await safeJson(res)};
    },
    post: async (url, body, {signal} = {}) => {
        const res = await fetch(url, {
            method: 'POST',
            headers: buildHeaders(),
            body: JSON.stringify(body),
            signal
        });
        return {ok: res.ok, status: res.status, json: await safeJson(res)};
    },
    put: async (url, body, {signal} = {}) => {
        const res = await fetch(url, {
            method: 'PUT',
            headers: buildHeaders(),
            body: JSON.stringify(body),
            signal
        });
        return {ok: res.ok, status: res.status, json: await safeJson(res)};
    },
    del: async (url, {signal} = {}) => {
        const res = await fetch(url, {method: 'DELETE', headers: buildHeaders(), signal});
        return {ok: res.ok, status: res.status, json: await safeJson(res)};
    },
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
    show: (el, show = true) => {
        if (el) el.classList.toggle('d-none', !show);
    },
    hide: (el) => {
        if (el) el.classList.add('d-none');
    },
};

export const fmt = {
    escape: (s) => {
        if (s == null) return '';
        return String(s)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    },
    dateTime: (iso) => {
        if (!iso) return '';
        const d = new Date(iso);
        return d.toLocaleString('ru-RU', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });
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
            sp.innerHTML = `<div class="spinner-border" role="status" aria-label="Загрузка"></div>`;
            anchor.after(sp);
        }
        return sp;
    },
};
