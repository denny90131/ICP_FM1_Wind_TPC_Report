// 切換主題並儲存
function setAppTheme(themeName) {
    if (themeName === 'dark') {
        document.documentElement.removeAttribute('data-theme');
    } else {
        document.documentElement.setAttribute('data-theme', themeName);
    }
    localStorage.setItem('app_theme', themeName);
}

// 監聽載入事件
document.addEventListener('DOMContentLoaded', () => {
    const savedTheme = localStorage.getItem('app_theme');
    if (savedTheme) {
        setAppTheme(savedTheme);
    }
});