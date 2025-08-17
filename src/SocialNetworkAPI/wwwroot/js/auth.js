import { http, storage, dom, ui } from './shared.js';

const disableForm = (form, on) => {
    dom.$$('input,button', form).forEach(el => el.disabled = on);
};

document.addEventListener('DOMContentLoaded', () => {
    const loginForm = dom.$('#login-form');
    const registerForm = dom.$('#register-form');

    loginForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        disableForm(loginForm, true);
        const email = dom.$('#login-email')?.value.trim();
        const password = dom.$('#login-password')?.value;

        const { ok, json, resp } = await http.post('/api/auth/login', { login: email, password });
        if (ok) {
            const token = typeof json === 'string' ? json : (json?.token ?? await resp.text());
            storage.setToken(token);
            location.href = 'index.html';
        } else {
            ui.toast(json?.message || 'Ошибка входа');
        }
        disableForm(loginForm, false);
    });

    registerForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        disableForm(registerForm, true);

        const data = {
            firstName: dom.$('#reg-firstname')?.value.trim(),
            lastName: dom.$('#reg-lastname')?.value.trim(),
            email: dom.$('#reg-email')?.value.trim(),
            password: dom.$('#reg-password')?.value,
            confirmedPassword: dom.$('#reg-confirm')?.value,
            dateOfBirth: dom.$('#reg-birthdate')?.value,
        };
        if (data.password !== data.confirmedPassword) {
            ui.toast('Пароли не совпадают'); disableForm(registerForm, false); return;
        }

        const { ok, json } = await http.post('/api/auth/register', data);
        if (ok) location.href = 'login.html';
        else ui.toast(json?.message || 'Ошибка регистрации');

        disableForm(registerForm, false);
    });
});
