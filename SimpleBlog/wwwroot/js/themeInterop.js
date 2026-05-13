window.themeInterop = {
    setTheme: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
    },
    getTheme: function () {
        return localStorage.getItem('theme') || 'light';
    }
};
