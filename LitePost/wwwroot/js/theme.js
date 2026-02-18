window.setTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
}

window.getTheme = () => {
    return localStorage.getItem('theme') || 'light';
}