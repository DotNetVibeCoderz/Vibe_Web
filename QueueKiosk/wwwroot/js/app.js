window.QueueKiosk = {
    print: function () {
        window.print();
    },
    setTheme: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
    }
};
