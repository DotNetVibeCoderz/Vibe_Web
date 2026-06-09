window.quillInterop = {
    init: function (element, placeholder) {
        var quill = new Quill(element, {
            modules: {
                toolbar: [
                    [{ header: [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    ['blockquote', 'code-block'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    ['link', 'image'],
                    ['clean']
                ]
            },
            placeholder: placeholder || 'Compose an epic...',
            theme: 'snow'
        });

        // Simpan instance quill ke element supaya bisa diakses nanti
        element.__quill = quill;

        quill.on('text-change', function () {
            element.dispatchEvent(new CustomEvent('change', { detail: quill.root.innerHTML }));
        });

        return true;
    },
    getContent: function (element) {
        return element.__quill ? element.__quill.root.innerHTML : "";
    },
    setContent: function (element, content) {
        if (element.__quill) {
            element.__quill.root.innerHTML = content;
        }
    }
};
