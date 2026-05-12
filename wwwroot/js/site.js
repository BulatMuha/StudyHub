// ============================================
// DIPLOM_STUDYHUB - JAVASCRIPT
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    initTheme();
    initNotifications();
    initChat();
    console.log('> StudyHub initialized successfully');
});

// ============================================
// ТЕМЫ (THEMES)
// ============================================

function initTheme() {
    const savedTheme = localStorage.getItem('studyhub-theme') || 'base';
    applyTheme(savedTheme);
    const themeSelector = document.getElementById('themeSelector');
    if (themeSelector) {
        themeSelector.value = savedTheme;
        themeSelector.addEventListener('change', function (e) {
            const newTheme = e.target.value;
            localStorage.setItem('studyhub-theme', newTheme);
            applyTheme(newTheme);
            showNotification('success', 'Тема изменена', 'Тема "' + getThemeName(newTheme) + '" применена');
        });
    }
}

function applyTheme(themeName) {
    document.documentElement.setAttribute('data-theme', themeName);
}

function getThemeName(theme) {
    const themes = { 'base': 'Base (Зелёная)', 'dark': 'Dark (Синяя)', 'light': 'Light (Светлая)' };
    return themes[theme] || theme;
}

function switchTheme(themeName) {
    applyTheme(themeName);
    localStorage.setItem('studyhub-theme', themeName);
    const themeSelector = document.getElementById('themeSelector');
    if (themeSelector) themeSelector.value = themeName;
}

// ============================================
// УВЕДОМЛЕНИЯ (NOTIFICATIONS)
// ============================================

function initNotifications() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            alert.style.transition = 'opacity 0.3s ease';
            alert.style.opacity = '0';
            setTimeout(function () { alert.remove(); }, 300);
        }, 5000);
    });
}

function showNotification(type, title, message) {
    const container = document.querySelector('.notifications-container') || createNotificationsContainer();
    const notification = document.createElement('div');
    notification.className = 'notification-card ' + type;
    notification.innerHTML = '<div class="notification-card-header"><span class="notification-card-title">' + title + '</span><button class="btn btn-sm btn-secondary" onclick="this.closest(\'.notification-card\').remove()">×</button></div><div class="notification-card-body">' + message + '</div>';
    container.appendChild(notification);
    setTimeout(function () {
        notification.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
        notification.style.opacity = '0';
        notification.style.transform = 'translateX(100px)';
        setTimeout(function () { notification.remove(); }, 300);
    }, 5000);
}

function createNotificationsContainer() {
    const container = document.createElement('div');
    container.className = 'notifications-container';
    container.style.cssText = 'position: fixed; top: 80px; right: 20px; z-index: 9999; max-width: 400px;';
    document.body.appendChild(container);
    return container;
}

// ============================================
// ЧАТ (CHAT)
// ============================================

function initChat() {
    const chatMessages = document.querySelector('.chat-messages');
    if (chatMessages) chatMessages.scrollTop = chatMessages.scrollHeight;
}

function sendMessage(messageText) {
    const chatMessages = document.querySelector('.chat-messages');
    if (!chatMessages || !messageText.trim()) return;
    const message = document.createElement('div');
    message.className = 'chat-message own';
    const time = new Date().toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
    message.innerHTML = '<div class="chat-message-header"><span class="chat-message-sender">Вы</span><span class="chat-message-time">' + time + '</span></div><div class="chat-message-text">' + escapeHtml(messageText) + '</div>';
    chatMessages.appendChild(message);
    chatMessages.scrollTop = chatMessages.scrollHeight;
    const input = document.querySelector('.chat-input');
    if (input) input.value = '';
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ============================================
// УТИЛИТЫ (UTILITIES)
// ============================================

async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        showNotification('success', 'Скопировано', 'Текст скопирован в буфер обмена');
    } catch (err) {
        const textArea = document.createElement('textarea');
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showNotification('success', 'Скопировано', 'Текст скопирован в буфер обмена');
    }
}

function confirmAction(message, callback) {
    if (confirm(message)) callback();
}

function showLoading() {
    let loader = document.getElementById('global-loader');
    if (!loader) {
        loader = document.createElement('div');
        loader.id = 'global-loader';
        loader.className = 'global-loader';
        loader.innerHTML = '<div class="loader-content"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Загрузка...</span></div><p>Загрузка...</p></div>';
        loader.style.cssText = 'position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(13, 17, 23, 0.8); display: flex; align-items: center; justify-content: center; z-index: 99999;';
        document.body.appendChild(loader);
    }
    loader.style.display = 'flex';
}

function hideLoading() {
    const loader = document.getElementById('global-loader');
    if (loader) loader.remove();
}

function validateForm(form) {
    const inputs = form.querySelectorAll('input[required], textarea[required], select[required]');
    let isValid = true;
    inputs.forEach(function (input) {
        if (!input.value.trim()) { input.classList.add('is-invalid'); isValid = false; }
        else { input.classList.remove('is-invalid'); }
    });
    return isValid;
}

const style = document.createElement('style');
style.textContent = '.is-invalid { border-color: var(--accent-danger) !important; box-shadow: 0 0 0 3px rgba(218, 54, 51, 0.1) !important; }';
document.head.appendChild(style);

// ============================================
// КОНСОЛЬНЫЙ ЛОГОТИП
// ============================================

console.log('%c> Diplom_StudyHub', 'color: #238636; font-size: 24px; font-weight: bold; font-family: monospace;');
console.log('%cLearning Environment', 'color: #8b949e; font-size: 12px; font-family: monospace;');
console.log('%cVersion 1.0.0', 'color: #6e7681; font-size: 10px; font-family: monospace;');

// ============================================
// Глобальные улучшения
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    // Авто-скрытие alert
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.4s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 400);
        }, 6000);
    });

    // Подтверждение опасных действий
    document.querySelectorAll('form[onsubmit*="confirm"]').forEach(form => {
        form.addEventListener('submit', function (e) {
            if (!confirm('Вы уверены? Это действие нельзя отменить.')) {
                e.preventDefault();
            }
        });
    });

    console.log('%cStudyHub успешно загружен', 'color: #238636; font-weight: bold;');
});