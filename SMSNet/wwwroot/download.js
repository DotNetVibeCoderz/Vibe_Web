// File download helpers.
// The BOM matters: without it Excel opens UTF-8 CSV as ANSI and mangles every
// Indonesian name with a diacritic.
window.smsnetDownload = function (fileName, content, mime) {
    const type = mime || 'text/csv;charset=utf-8;';
    const isCsv = type.indexOf('csv') !== -1;
    const parts = isCsv ? ['﻿', content] : [content];

    const blob = new Blob(parts, { type: type });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
};

window.smsnetPrint = function () {
    window.print();
};

window.smsnetCopy = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (e) {
        return false;
    }
};
