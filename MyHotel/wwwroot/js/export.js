window.downloadFile = function(fileName, contentType, content) {
    var blob = new Blob([content], { type: contentType });
    var link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
    link.click();
};