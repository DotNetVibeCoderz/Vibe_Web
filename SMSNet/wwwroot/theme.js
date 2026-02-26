window.smsnetTheme = {
    toggle: function () {
        const root = document.documentElement;
        const isDark = root.classList.toggle('dark');
        if (isDark) {
            root.classList.remove('light');
            localStorage.setItem('smsnet-theme', 'dark');
        } else {
            root.classList.add('light');
            localStorage.setItem('smsnet-theme', 'light');
        }
    },
    load: function () {
        const saved = localStorage.getItem('smsnet-theme');
        if (saved === 'dark') {
            document.documentElement.classList.add('dark');
            document.documentElement.classList.remove('light');
        } else {
            document.documentElement.classList.remove('dark');
            document.documentElement.classList.add('light');
        }
    }
};

window.smsnetTheme.load();
