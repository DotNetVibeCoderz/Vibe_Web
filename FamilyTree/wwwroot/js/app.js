window.familyTreeTheme = {
    getTheme: () => {
        const theme = localStorage.getItem('ft-theme') || 'light';
        document.documentElement.setAttribute('data-theme', theme);
        return theme;
    },
    toggleTheme: () => {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const next = current === 'light' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('ft-theme', next);
        return next;
    }
};

window.familyTreeDownload = (fileName, content) => {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
};

window.familyTreeExport = {
    downloadText: (fileName, content, mimeType) => {
        const blob = new Blob([content], { type: mimeType || 'text/plain' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
    },
    printHtml: (title, html) => {
        const win = window.open('', '_blank');
        if (!win) {
            return;
        }
        win.document.write(html);
        win.document.close();
        win.document.title = title;
        win.focus();
        win.print();
    }
};
