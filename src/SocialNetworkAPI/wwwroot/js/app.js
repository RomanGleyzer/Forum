import { storage, dom } from './shared.js';

document.addEventListener('DOMContentLoaded', () => {
    const token = storage.getToken();
    const isAuth = !!token;

    const currentPage = location.pathname.split('/').pop();
    const protectedPages = new Set(['', 'index.html', 'profile.html']);
    const publicPages = new Set(['login.html', 'register.html']);

    if (!isAuth && protectedPages.has(currentPage)) {
        location.href = 'login.html';
        return;
    }
    if (isAuth && publicPages.has(currentPage)) {
        location.href = 'index.html';
        return;
    }

    const loginNav = dom.$('#login-nav');
    const registerNav = dom.$('#register-nav');
    const profileNav = dom.$('#profile-nav');
    const logoutNav = dom.$('#logout-nav');
    const createPost = dom.$('#create-post');

    dom.show(loginNav, !isAuth);
    dom.show(registerNav, !isAuth);
    dom.show(profileNav, isAuth);
    dom.show(logoutNav, isAuth);
    dom.show(createPost, isAuth);

    logoutNav?.addEventListener('click', (e) => {
        e.preventDefault();
        storage.clearToken();
        location.href = 'login.html';
    }, { passive: true });
});
