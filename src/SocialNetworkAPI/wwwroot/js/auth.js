document.addEventListener('DOMContentLoaded', () => {
    
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const email = document.getElementById('login-email').value;
            const password = document.getElementById('login-password').value;

            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    login: email,
                    password: password
                })
            });

            if (response.ok) {
                const token = await response.text();
                localStorage.setItem('token', token);
                window.location.href = 'index.html';
            } else {
                const error = await response.json();
                alert(error.message || 'Ошибка входа');
            }
        });
    }

    const registerForm = document.getElementById('register-form');
    if (registerForm) {
        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const firstName = document.getElementById('reg-firstname').value;
            const lastName = document.getElementById('reg-lastname').value;
            const email = document.getElementById('reg-email').value;
            const password = document.getElementById('reg-password').value;
            const confirmPassword = document.getElementById('reg-confirm').value;
            const dateOfBirth = document.getElementById('reg-birthdate').value;

            if (password !== confirmPassword) {
                alert('Пароли не совпадают');
                return;
            }

            const response = await fetch('/api/auth/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    firstName: firstName,
                    lastName: lastName,
                    email: email,
                    password: password,
                    confirmedPassword: confirmPassword,
                    dateOfBirth: dateOfBirth
                })
            });

            if (response.ok) {
                window.location.href = 'login.html';
            } else {
                const error = await response.json();
                alert(error.message || 'Ошибка регистрации');
            }
        });
    }
});
