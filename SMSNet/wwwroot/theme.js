// Theme control. The initial resolution happens in a blocking <head> script so
// the page never paints the wrong scheme; this file owns everything after that.
window.smsnetTheme = {
    current: function () {
        return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
    },

    apply: function (theme) {
        const root = document.documentElement;
        const dark = theme === 'dark';
        root.classList.toggle('dark', dark);
        root.classList.toggle('light', !dark);
        try { localStorage.setItem('smsnet-theme', theme); } catch (e) { }
        window.dispatchEvent(new CustomEvent('smsnet:themechange', { detail: { theme } }));
        return theme;
    },

    toggle: function () {
        return window.smsnetTheme.apply(window.smsnetTheme.current() === 'dark' ? 'light' : 'dark');
    }
};

// Follow the OS only while the user hasn't expressed a preference of their own.
try {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem('smsnet-theme')) {
            window.smsnetTheme.apply(e.matches ? 'dark' : 'light');
        }
    });
} catch (e) { }

// Off-canvas sidebar: close on Escape, and trap the scrim click.
window.smsnetShell = {
    lockScroll: function (locked) {
        document.body.style.overflow = locked ? 'hidden' : '';
    }
};
