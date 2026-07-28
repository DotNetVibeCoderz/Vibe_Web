/* ============================================================
   VirtualDoctor - perilaku UI dasar
   Tema disimpan di localStorage dan diterapkan sebelum render
   pertama (lihat App.razor) agar tidak ada kedipan warna.
   ============================================================ */
(function () {
    "use strict";

    const KEY = "vd-theme";

    function systemTheme() {
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function apply(theme) {
        const t = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", t);
        // kelas lama tetap dipakai sebagian komponen
        document.documentElement.classList.toggle("dark-theme", t === "dark");
        document.body && document.body.classList.toggle("dark-theme", t === "dark");
        document.dispatchEvent(new CustomEvent("vd:themechange", { detail: { theme: t } }));
        return t;
    }

    window.vdApp = {
        initTheme() {
            const stored = localStorage.getItem(KEY);
            return apply(stored || systemTheme());
        },
        getTheme() {
            return document.documentElement.getAttribute("data-theme") || systemTheme();
        },
        setTheme(theme) {
            localStorage.setItem(KEY, theme);
            return apply(theme);
        },
        toggleTheme() {
            return this.setTheme(this.getTheme() === "dark" ? "light" : "dark");
        },

        /** Kunci scroll body saat drawer/modal terbuka. */
        lockScroll(on) {
            document.body.style.overflow = on ? "hidden" : "";
        },

        /** Scroll ke elemen terakhir dalam wadah (dipakai chat). */
        scrollToEnd(id) {
            const el = document.getElementById(id);
            if (el) el.scrollTop = el.scrollHeight;
        },

        /** Unduh string sebagai berkas - dipakai ekspor tabel dashboard. */
        downloadText(filename, text, mime) {
            const blob = new Blob([text], { type: mime || "text/csv;charset=utf-8;" });
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        },

        openInNewTab(url) {
            window.open(url, "_blank", "noopener");
        },

        /** Buka dialog cetak. Dipakai halaman invoice dan kuitansi. */
        printPage() {
            window.print();
        },

        async copyText(text) {
            try { await navigator.clipboard.writeText(text); return true; }
            catch { return false; }
        }
    };

    // ikuti perubahan tema sistem selama pengguna belum memilih manual
    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", (e) => {
            if (!localStorage.getItem(KEY)) apply(e.matches ? "dark" : "light");
        });
    }
})();
