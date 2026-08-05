# Rich Text, Comments & Uploads

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/kolaborasi.md)

---

![Forum with comments](../img/forum-komentar.png)

Three capabilities shared across several pages: a **rich-text editor**, **comment
threads**, and **file uploads**.

---

## 1. Rich-Text Editor

![Rich-text editor](../img/editor-teks.png)

Used on **Komunikasi Internal** (internal forum) for the body of a topic.

| Group | Contents |
| --- | --- |
| Text styles | Bold, Italic, Underline, Strikethrough |
| Blocks | Heading, Quote, Code |
| Lists | Bulleted, Numbered |
| Other | Insert link, Clear formatting |

### Things worth knowing

**Pasting always arrives as plain text.** Copying from Word or a web page otherwise
drags in font tags, class names, and colours that fight the app's own styling. The
formatting is dropped; the content is kept.

**Links are validated.** An address without a scheme is treated as `https://`, and only
`http`/`https` are accepted.

**The editor ships with the app rather than coming from a CDN.** The app does load
Tailwind and Chart.js from CDNs — a page is still readable without either. An editor
that disappears when the network hiccups means somebody loses what they wrote, so this
one is hand-written (~150 lines) and served locally.

### Older content still reads correctly

Topics written before the editor existed are stored as plain text with real newlines.
On display, such values are detected and their lines promoted to paragraphs — without
that, every line would run together.

### Security

The editor runs in the browser, so its output is **user input like any other**: a
hand-crafted request can post whatever HTML it likes without touching the toolbar. So
content is **sanitised on save, not on display** — a payload never reaches storage in
the first place.

Stripped: `script`, `iframe`, event attributes, `style` and `class` attributes, and any
scheme other than `http`, `https`, and `mailto`. Outbound links automatically get
`target="_blank"` and `rel="noopener noreferrer"`.

---

## 2. Comment Threads

Available on:

| Page | What is commented on |
| --- | --- |
| **Komunikasi Internal** | Each discussion topic |
| **Evaluasi Kinerja** | Each KPI indicator |

A comment may carry text, **emoji** (20 quick picks), and **attachments** — images
appear as thumbnails, other files as a download link with its size.

### Who may delete

> **A comment can only be deleted by its author, or by an admin.**

The rule is enforced in **`CommentService.DeleteAsync`**, not merely by hiding the
button. A hidden button is only the first layer; a page that forgot to check would be
an invisible hole.

Ownership is matched on **account id**, never on display name — names are neither
unique nor fixed.

### Deleting the host

Deleting a topic or an indicator also deletes its comments and their attached files.
Threads are addressed by a (type, id) pair rather than a foreign key, so the database
cannot cascade on its own — without the explicit delete, the next record to reuse that
id would inherit a stranger's discussion.

---

## 3. File Uploads

![Document upload](../img/dokumen-unggah.png)

Used on **Dokumen Digital** and for comment attachments.

Dokumen Digital offers two modes:

| Mode | When to use it |
| --- | --- |
| **Unggah berkas** (upload) | The school holds the file itself |
| **Tautan** (link) | The document lives elsewhere; the school records only its address |

The list marks the two with different icons, and deleting a record removes the physical
file only when this app is the one storing it. The confirmation dialog says which case
applies.

### Limits

| Setting | Default | appsettings key |
| --- | --- | --- |
| Maximum size | 15 MB | `Uploads:MaxFileSizeBytes` |
| Attachments per comment | 5 | `Uploads:MaxFilesPerItem` |
| Allowed types | pdf, doc(x), xls(x), ppt(x), txt, csv, md, rtf, odt, images, zip | `Uploads:AllowedExtensions` |

### The client's filename is never trusted

The name supplied by the browser **never touches the filesystem**. It can contain path
traversal (`../../appsettings.json`), and two users uploading `rapor.pdf` would
overwrite each other. The stored name is generated; only an allowlisted extension is
carried across. The original name is kept separately, for display.

---

## Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| Editor toolbar does nothing | `editor.js` failed to load | Check the browser console; the file is at `wwwroot/editor.js` |
| Formatting disappears after saving | That tag is outside the sanitiser allowlist | Expected — see the allowed tags above |
| No delete button on a comment | Not the author and not an admin | Working as intended |
| Upload rejected | Extension is not allowlisted | Add it to `Uploads:AllowedExtensions` if genuinely needed |
| Uploaded file 404s | The file was removed by hand from `wwwroot/uploads` | Re-upload via the edit button |
