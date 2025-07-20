document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('token');
    const isAuth = !!token;

    const currentPage = window.location.pathname.split('/').pop();

    const protectedPages = [
        'index.html',
        'profile.html',
        ''
    ];

    const publicPages = [
        'login.html',
        'register.html'
    ];

    if (!isAuth && protectedPages.includes(currentPage)) {
        window.location.href = 'login.html';
        return;
    }

    const loginNav = document.getElementById('login-nav');
    const registerNav = document.getElementById('register-nav');
    const profileNav = document.getElementById('profile-nav');
    const logoutNav = document.getElementById('logout-nav');
    const createPost = document.getElementById('create-post');

    if (loginNav) loginNav.style.display = isAuth ? 'none' : '';
    if (registerNav) registerNav.style.display = isAuth ? 'none' : '';
    if (profileNav) profileNav.style.display = isAuth ? '' : 'none';
    if (logoutNav) logoutNav.style.display = isAuth ? '' : 'none';
    if (createPost) createPost.style.display = isAuth ? '' : 'none';

    if (logoutNav) {
        logoutNav.addEventListener('click', (e) => {
            e.preventDefault();
            localStorage.removeItem('token');
            window.location.href = 'login.html';
        });
    }
});