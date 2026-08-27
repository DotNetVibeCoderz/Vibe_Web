// Small browser-side helpers. Kept to the things Blazor Server cannot do on its
// own: reading the stored theme, and scrolling the chat transcript.
window.lapak = {
    getTheme: function () {
        try {
            var stored = localStorage.getItem('lapak-theme');
            if (stored === 'dark' || stored === 'light') return stored;
            return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        } catch (e) {
            return 'light';
        }
    },

    // Stores the choice and flips the class on <html>, which is what makes the
    // page background follow the theme — the Blazor layout sits inside <body>.
    setTheme: function (theme) {
        try {
            localStorage.setItem('lapak-theme', theme);
        } catch (e) {
            /* private mode or blocked storage — the theme still applies for this session */
        }
        document.documentElement.classList.toggle('dark-theme', theme === 'dark');
    },

    scrollToBottom: function (elementId) {
        var el = document.getElementById(elementId);
        if (el) el.scrollTop = el.scrollHeight;
    },

    // Grows the chat textarea with its content, up to the CSS max-height.
    autoGrow: function (element) {
        if (!element) return;
        element.style.height = 'auto';
        element.style.height = Math.min(element.scrollHeight, 120) + 'px';
    }
};
