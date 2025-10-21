import {dom, http, storage, ui} from './shared.js';

const disableForm = (form, d) => {
    form?.querySelectorAll('input,button,select,textarea').forEach(el => el.disabled = !!d);
};

const loginForm = dom.$('#login-form');
const registerForm = dom.$('#register-form');

loginForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    disableForm(loginForm, true);

    const email = dom.$('#login-email')?.value.trim();
    const password = dom.$('#login-password')?.value;

    const {ok, json} = await http.post('/api/auth/login', {email, password});

    if (ok) {
        const token =
            json?.accessToken ??
            json?.token ??
            (typeof json === 'string' ? json : null);

        if (!token) {
            ui.toast('Сервис вернул неожиданный ответ');
            disableForm(loginForm, false);
            return;
        }

        storage.setToken(token);
        location.href = 'index.html';
    } else {
        ui.toast(json?.message || 'Неверный email или пароль');
        disableForm(loginForm, false);
    }
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
        ui.toast('Пароли не совпадают');
        disableForm(registerForm, false);
        return;
    }

    const {ok, json} = await http.post('/api/auth/register', data);
    if (ok) {
        ui.toast('Аккаунт создан. Выполните вход.', 'success');
        location.href = 'login.html';
    } else {
        ui.toast(json?.message || 'Не удалось зарегистрироваться');
        disableForm(registerForm, false);
    }
});
