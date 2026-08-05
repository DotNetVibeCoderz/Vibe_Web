// Camera QR scanning for the attendance screen.
//
// Uses the browser's own BarcodeDetector where available (Chrome, Edge, and Android
// WebView — which covers the machines a school gate actually runs), and falls back to
// jsQR decoding video frames on a canvas everywhere else. Both paths hand the decoded
// text to the same .NET callback, so the Blazor side never needs to know which ran.
window.smsnetQrScan = (function () {
    let stream = null;
    let video = null;
    let canvas = null;
    let ctx = null;
    let detector = null;
    let rafId = null;
    let timerId = null;
    let dotnet = null;
    let running = false;

    // The same card lingers in frame for many frames; without this the operator would
    // get a burst of identical scans for one tap.
    let lastText = '';
    let lastAt = 0;
    const REPEAT_BLOCK_MS = 2500;

    function stopTracks() {
        if (stream) {
            stream.getTracks().forEach(t => t.stop());
            stream = null;
        }
    }

    async function emit(text) {
        const now = Date.now();
        if (!text) return;
        if (text === lastText && now - lastAt < REPEAT_BLOCK_MS) return;

        lastText = text;
        lastAt = now;

        // A short beep confirms the read without the operator watching the screen.
        try {
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            if (AudioCtx) {
                const ac = new AudioCtx();
                const osc = ac.createOscillator();
                const gain = ac.createGain();
                osc.frequency.value = 880;
                gain.gain.value = 0.06;
                osc.connect(gain); gain.connect(ac.destination);
                osc.start(); osc.stop(ac.currentTime + 0.08);
                setTimeout(() => ac.close(), 300);
            }
        } catch (e) { /* audio is a nicety, never a blocker */ }

        if (navigator.vibrate) { try { navigator.vibrate(40); } catch (e) { } }

        if (dotnet) {
            try { await dotnet.invokeMethodAsync('OnScanned', text); }
            catch (e) { /* circuit torn down mid-scan */ }
        }
    }

    async function scanNative() {
        if (!running || !video || video.readyState < 2) {
            rafId = requestAnimationFrame(scanNative);
            return;
        }
        try {
            const codes = await detector.detect(video);
            if (codes && codes.length) await emit(codes[0].rawValue);
        } catch (e) { /* transient decode failure — keep going */ }
        rafId = requestAnimationFrame(scanNative);
    }

    function scanFallback() {
        if (!running) return;
        if (video && video.readyState >= 2 && window.jsQR) {
            const w = video.videoWidth, h = video.videoHeight;
            if (w && h) {
                canvas.width = w; canvas.height = h;
                ctx.drawImage(video, 0, 0, w, h);
                try {
                    const img = ctx.getImageData(0, 0, w, h);
                    const code = window.jsQR(img.data, w, h, { inversionAttempts: 'dontInvert' });
                    if (code && code.data) emit(code.data);
                } catch (e) { }
            }
        }
        // 10 fps is plenty for a card held still, and far kinder to a cheap gate laptop
        // than decoding every frame.
        timerId = setTimeout(scanFallback, 100);
    }

    return {
        /**
         * @param {string} videoId  id of the <video> element
         * @param {object} dotnetRef DotNetObjectReference exposing OnScanned(string)
         * @returns {Promise<{ok:boolean, engine?:string, error?:string}>}
         */
        start: async function (videoId, dotnetRef) {
            await this.stop();

            video = document.getElementById(videoId);
            if (!video) return { ok: false, error: 'Elemen kamera tidak ditemukan.' };

            dotnet = dotnetRef;
            lastText = '';

            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                return { ok: false, error: 'Peramban ini tidak mendukung akses kamera. Gunakan mode Scanner / Ketik Manual.' };
            }

            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } },
                    audio: false
                });
            } catch (e) {
                const map = {
                    NotAllowedError: 'Izin kamera ditolak. Berikan izin pada peramban.',
                    NotFoundError: 'Tidak ada kamera yang terdeteksi pada perangkat ini.',
                    NotReadableError: 'Kamera sedang dipakai aplikasi lain.',
                    SecurityError: 'Akses kamera memerlukan HTTPS (atau localhost).',
                    OverconstrainedError: 'Kamera perangkat ini tidak memenuhi resolusi yang diminta.'
                };
                // Whatever went wrong, the operator needs the way forward — the manual
                // mode always works, so every message ends by pointing at it.
                const reason = map[e.name] || ('Kamera tidak dapat dibuka (' + (e.message || e.name) + ').');
                return { ok: false, error: reason + ' Gunakan mode Scanner / Ketik Manual.' };
            }

            video.srcObject = stream;
            video.setAttribute('playsinline', 'true');
            video.muted = true;
            try { await video.play(); } catch (e) { }

            running = true;

            if ('BarcodeDetector' in window) {
                try {
                    const formats = await window.BarcodeDetector.getSupportedFormats();
                    if (formats.includes('qr_code')) {
                        detector = new window.BarcodeDetector({ formats: ['qr_code'] });
                        rafId = requestAnimationFrame(scanNative);
                        return { ok: true, engine: 'BarcodeDetector' };
                    }
                } catch (e) { /* fall through to jsQR */ }
            }

            if (!window.jsQR) {
                await this.stop();
                return { ok: false, error: 'Pustaka pemindai gagal dimuat. Periksa koneksi internet, atau gunakan mode Scanner / Ketik Manual.' };
            }

            canvas = document.createElement('canvas');
            ctx = canvas.getContext('2d', { willReadFrequently: true });
            scanFallback();
            return { ok: true, engine: 'jsQR' };
        },

        stop: async function () {
            running = false;
            if (rafId) { cancelAnimationFrame(rafId); rafId = null; }
            if (timerId) { clearTimeout(timerId); timerId = null; }
            stopTracks();
            if (video) { try { video.srcObject = null; } catch (e) { } }
            detector = null;
            return true;
        },

        /** Keeps the scanner input focused so a hardware reader's keystrokes land in it. */
        keepFocus: function (inputId) {
            const el = document.getElementById(inputId);
            if (!el) return;
            el.focus();
            // Re-focus when the operator clicks elsewhere; a gate reader types blind.
            if (!el.dataset.refocus) {
                el.dataset.refocus = '1';
                el.addEventListener('blur', () => setTimeout(() => {
                    if (document.body.contains(el) && el.dataset.refocus === '1') el.focus();
                }, 120));
            }
        },

        releaseFocus: function (inputId) {
            const el = document.getElementById(inputId);
            if (el) el.dataset.refocus = '0';
        }
    };
})();
