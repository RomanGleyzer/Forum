let currentUserProfile = null;

function getAuthToken() {
    return localStorage.getItem('token');
}

async function getCurrentUser() {
    if (currentUserProfile) return currentUserProfile;
    const token = getAuthToken();
    if (!token) throw new Error('Not authenticated');
    const response = await fetch('/api/users/me/profile', {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Ошибка загрузки профиля');
    currentUserProfile = await response.json();
    return currentUserProfile;
}

function fillProfile(user) {
    document.getElementById('profile-fullname').textContent = (user.firstName || '') + ' ' + (user.lastName || '');
    document.getElementById('profile-email').textContent = user.email || '';
    document.getElementById('profile-birthdate').textContent = user.dateOfBirth
        ? new Date(user.dateOfBirth).toLocaleDateString()
        : '';
    document.getElementById('profile-bio').textContent = user.about || '';
    if (user.avatarUrl)
        document.getElementById('profile-avatar').src = user.avatarUrl;
}

function fillEditProfileForm(user) {
    document.getElementById('firstName').value = user.firstName || '';
    document.getElementById('lastName').value = user.lastName || '';
    document.getElementById('email').value = user.email || '';
    document.getElementById('dateOfBirth').value = user.dateOfBirth
        ? user.dateOfBirth.slice(0, 10)
        : '';
    document.getElementById('about').value = user.about || '';
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const user = await getCurrentUser();
        fillProfile(user);
        fillEditProfileForm(user);
    } catch (err) {
        alert('Ошибка загрузки профиля: ' + err.message);
    }
});

document.getElementById('edit-profile-btn').addEventListener('click', () => {
    document.getElementById('update-user-error').classList.add('d-none');
    document.getElementById('update-user-success').classList.add('d-none');
    new bootstrap.Modal(document.getElementById('editProfileModal')).show();
});

document.getElementById('edit-profile-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const token = getAuthToken();
    if (!token) {
        alert('Необходима авторизация!');
        return;
    }
    document.getElementById('update-user-error').classList.add('d-none');
    document.getElementById('update-user-success').classList.add('d-none');

    const data = {
        firstName: document.getElementById('firstName').value.trim(),
        lastName: document.getElementById('lastName').value.trim(),
        email: document.getElementById('email').value.trim(),
        dateOfBirth: document.getElementById('dateOfBirth').value,
        about: document.getElementById('about').value.trim()
    };

    try {
        const response = await fetch('/api/users', {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            let errorText = 'Ошибка обновления профиля';
            try {
                const err = await response.json();
                errorText = err?.message || errorText;
            } catch { }
            document.getElementById('update-user-error').textContent = errorText;
            document.getElementById('update-user-error').classList.remove('d-none');
            return;
        }
        document.getElementById('update-user-success').textContent = 'Профиль успешно обновлен!';
        document.getElementById('update-user-success').classList.remove('d-none');
        currentUserProfile = { ...currentUserProfile, ...data };
        fillProfile(currentUserProfile);
        setTimeout(() => {
            bootstrap.Modal.getInstance(document.getElementById('editProfileModal')).hide();
        }, 1000);
    } catch (err) {
        document.getElementById('update-user-error').textContent = 'Ошибка соединения с сервером';
        document.getElementById('update-user-error').classList.remove('d-none');
    }
});
