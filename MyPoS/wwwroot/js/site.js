window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
};

window.setBodyClass = (className) => {
    document.body.className = className;
};

/* Kasir memindai barcode jauh lebih sering daripada mengetik, jadi fokus dikembalikan
   ke kolom pindai setiap kali satu barang selesai ditambahkan. */
window.focusElement = (selector) => {
    const el = document.querySelector(selector);
    if (el) {
        el.focus();
        if (typeof el.select === 'function') el.select();
    }
};

/* Hanya elemen #print-area yang terlihat saat mencetak (lihat @media print). */
window.printReceipt = () => {
    window.print();
};

window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
};

window.openInNewTab = (url) => {
    window.open(url, '_blank', 'noopener');
};

/* Preferensi ringan per peramban (mode gelap, lebar drawer). Dibungkus try/catch
   karena localStorage bisa diblokir di mode privat. */
window.localPref = {
    get: (key) => {
        try { return localStorage.getItem(key); } catch { return null; }
    },
    set: (key, value) => {
        try { localStorage.setItem(key, value); } catch { /* diabaikan */ }
    }
};
