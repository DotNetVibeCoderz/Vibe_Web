// Minimal rich-text editor over a contenteditable div.
//
// Deliberately hand-rolled rather than pulled from a CDN: every other asset here
// is either local or a CDN script the page can survive losing, and a text editor
// that vanishes offline would silently cost someone their post. This is ~100
// lines against a dependency measured in hundreds of kilobytes.
//
// document.execCommand is formally deprecated but remains the only API that every
// browser implements for this, and its replacement was never shipped.
window.smsnetEditor = {
    _editors: {},

    init(id, dotNetRef, initialHtml) {
        const el = document.getElementById(id);
        if (!el) return;

        // Re-initialising the same element would stack duplicate listeners, and each
        // keystroke would then fire the .NET callback once per init.
        if (this._editors[id]) {
            this.destroy(id);
        }

        // Seed an empty paragraph. Typing into a bare contenteditable produces a
        // loose text node with no block around it, so the first line ends up outside
        // any element — and this markup is stored, not just displayed.
        el.innerHTML = initialHtml || '<p><br></p>';

        // The seeded <p><br></p> means :empty no longer matches, so emptiness is
        // marked with a class instead and the placeholder keys off that.
        const markEmpty = () => {
            const blank = el.textContent.trim() === '' && !el.querySelector('img, table');
            el.classList.toggle('is-kosong', blank);
        };

        const push = () => {
            markEmpty();

            try {
                dotNetRef.invokeMethodAsync('OnHtmlChanged', el.innerHTML);
            } catch {
                // Circuit went away mid-edit; nothing useful to do here.
            }
        };

        const onInput = () => push();

        markEmpty();

        // Paste as plain text: pasting from Word or a web page otherwise drags in
        // font tags, class names, and colours that fight the app's own styling.
        const onPaste = (e) => {
            e.preventDefault();
            const text = (e.clipboardData || window.clipboardData).getData('text/plain');
            document.execCommand('insertText', false, text);
        };

        // Enter inside a contenteditable produces <div> in some browsers and <p> in
        // others; forcing a paragraph keeps the stored markup predictable.
        const onKeyDown = (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                const inList = document.queryCommandState('insertUnorderedList') ||
                               document.queryCommandState('insertOrderedList');
                if (!inList) {
                    e.preventDefault();
                    document.execCommand('insertParagraph');
                }
            }
        };

        // Remember where the caret was. Opening the link dialog moves focus into a
        // text box, and the browser discards the editor's selection when it does —
        // so without this, inserting a link would apply it nowhere.
        const onSelectionChange = () => {
            const sel = document.getSelection();
            if (sel && sel.rangeCount > 0 && el.contains(sel.anchorNode)) {
                entry.savedRange = sel.getRangeAt(0).cloneRange();
            }
        };

        el.addEventListener('input', onInput);
        el.addEventListener('paste', onPaste);
        el.addEventListener('keydown', onKeyDown);
        el.addEventListener('blur', push);
        document.addEventListener('selectionchange', onSelectionChange);

        const entry = { el, onInput, onPaste, onKeyDown, push, onSelectionChange, savedRange: null };
        this._editors[id] = entry;

        try {
            document.execCommand('defaultParagraphSeparator', false, 'p');
        } catch { /* not supported everywhere; harmless */ }
    },

    // Puts focus and the caret back where the user left them. Focus alone is not
    // enough: a refocused contenteditable can land the caret at position zero, so
    // pressing "bullet list" and typing would drop the text at the top of the field.
    _restore(entry) {
        entry.el.focus();

        const sel = document.getSelection();
        if (!sel) return;

        const inside = sel.rangeCount > 0 && entry.el.contains(sel.anchorNode);
        if (inside || !entry.savedRange) return;

        sel.removeAllRanges();
        sel.addRange(entry.savedRange);
    },

    exec(id, command, value) {
        const entry = this._editors[id];
        if (!entry) return;

        this._restore(entry);
        document.execCommand(command, false, value || null);
        entry.push();
    },

    // Wraps the selection in a link. Kept separate from exec so the URL can be
    // validated before it reaches the document.
    link(id, url) {
        const entry = this._editors[id];
        if (!entry || !url) return;

        this._restore(entry);

        if (window.getSelection().isCollapsed) {
            // Nothing selected: insert the URL as its own link rather than silently
            // doing nothing, which is what execCommand would do.
            document.execCommand('insertHTML', false,
                `<a href="${url.replace(/"/g, '&quot;')}">${url.replace(/</g, '&lt;')}</a>`);
        } else {
            document.execCommand('createLink', false, url);
        }

        entry.push();
    },

    setHtml(id, html) {
        const entry = this._editors[id];
        if (!entry) return;

        // Only write when it actually differs: assigning innerHTML collapses the
        // caret to the start, which would fight the user on every keystroke.
        if (entry.el.innerHTML !== html) {
            entry.el.innerHTML = html || '<p><br></p>';
            entry.el.classList.toggle('is-kosong', entry.el.textContent.trim() === '');
        }
    },

    getHtml(id) {
        const entry = this._editors[id];
        return entry ? entry.el.innerHTML : '';
    },

    destroy(id) {
        const entry = this._editors[id];
        if (!entry) return;

        entry.el.removeEventListener('input', entry.onInput);
        entry.el.removeEventListener('paste', entry.onPaste);
        entry.el.removeEventListener('keydown', entry.onKeyDown);
        entry.el.removeEventListener('blur', entry.push);
        document.removeEventListener('selectionchange', entry.onSelectionChange);

        delete this._editors[id];
    }
};
