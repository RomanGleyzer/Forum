document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('token');
    if (token) {
        const response = await fetch('/api/users/me', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (response.ok) {
            const user = await response.json();
            document.getElementById('sidebar-username').textContent = user.firstName + ' ' + user.lastName;
            if (user.avatarUrl) {
                document.getElementById('sidebar-avatar').src = user.avatarUrl;
            }
        }
    }
});
