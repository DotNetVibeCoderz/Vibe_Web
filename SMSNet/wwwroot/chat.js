// Chat surface behaviours for the Pak Dedi assistant.
window.smsnetChat = {
    // Only pin to the bottom if the reader is already near it — otherwise a
    // streaming reply would yank them away from what they were reading.
    scrollToBottom: function (elementId, force) {
        const el = document.getElementById(elementId);
        if (!el) return;
        const distance = el.scrollHeight - el.scrollTop - el.clientHeight;
        if (force || distance < 220) {
            el.scrollTo({ top: el.scrollHeight, behavior: force ? 'auto' : 'smooth' });
        }
    },

    autoGrow: function (elementId, maxPx) {
        const el = document.getElementById(elementId);
        if (!el) return;
        el.style.height = 'auto';
        el.style.height = Math.min(el.scrollHeight, maxPx || 200) + 'px';
    },

    focus: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) el.focus();
    },

    // Attach a copy button to every code block rendered from markdown.
    enhanceCode: function (rootId) {
        const root = document.getElementById(rootId);
        if (!root) return;

        root.querySelectorAll('pre > code').forEach(function (code) {
            const pre = code.parentElement;
            if (pre.dataset.enhanced) return;
            pre.dataset.enhanced = '1';
            pre.style.position = 'relative';

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'sms-code-copy';
            btn.textContent = 'Salin';
            btn.addEventListener('click', async function () {
                try {
                    await navigator.clipboard.writeText(code.innerText);
                    btn.textContent = 'Tersalin';
                    setTimeout(function () { btn.textContent = 'Salin'; }, 1600);
                } catch (e) {
                    btn.textContent = 'Gagal';
                }
            });
            pre.appendChild(btn);
        });
    }
};
