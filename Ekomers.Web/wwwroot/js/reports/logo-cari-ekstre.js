(function () {
    'use strict';
    const root = document.getElementById('logoCariReport');
    if (!root) return;
    function state(id, marker, path) {
        const dialog = document.getElementById(id);
        const s = { dialog, marker, path: new URL(path, window.location.href).pathname,
            content: dialog.querySelector('[data-cs-content]'), status: dialog.querySelector('[data-cs-status]'), sequence: 0 };
        dialog.querySelector('[data-cs-close]').addEventListener('click', () => dialog.close());
        dialog.addEventListener('close', () => {
            s.sequence++; if (s.pending) s.pending.abort();
            s.content.innerHTML = ''; s.content.setAttribute('aria-busy', 'false'); s.status.hidden = true;
            if (s.trigger && s.trigger.isConnected) s.trigger.focus();
        });
        s.content.addEventListener('click', event => {
            const page = event.target.closest('[data-cs-page]');
            if (!page) return;
            event.preventDefault(); load(s, page.href);
        });
        return s;
    }
    const statement = state('csStatementDialog', 'Ekstre', root.dataset.statementPath);
    const documentDetail = state('csDocumentDialog', 'Belge', root.dataset.documentPath);
    function safeUrl(s, value) {
        const url = new URL(value, window.location.href);
        if (url.origin !== window.location.origin || url.pathname !== s.path) throw new Error('Geçersiz rapor adresi.');
        return url;
    }
    async function load(s, value) {
        if (s.pending) s.pending.abort(); s.pending = new AbortController(); const sequence = ++s.sequence;
        s.content.innerHTML = ''; s.content.setAttribute('aria-busy', 'true'); s.status.hidden = false;
        s.status.classList.remove('is-error'); s.status.textContent = 'Logo kayıtları yükleniyor…';
        try {
            const url = safeUrl(s, value);
            const response = await fetch(url, { credentials: 'same-origin', cache: 'no-store', signal: s.pending.signal,
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'text/html' } });
            if (response.redirected || response.status === 401 || response.status === 403)
                throw new Error('Oturumunuz sona ermiş veya rapor yetkiniz yok. Sayfayı yenileyip tekrar deneyiniz.');
            if (response.status === 404) throw new Error('Bu firma/dönem/cari için kayıt bulunamadı. Listeyi yenileyiniz.');
            if (!response.ok || response.headers.get('X-Logo-Cari-Partial') !== s.marker)
                throw new Error('Logo kayıtları okunamadı. Filtreleri ve bağlantıyı kontrol edip tekrar deneyiniz.');
            const html = await response.text();
            if (s.sequence !== sequence || !s.dialog.open) return;
            // Only authenticated server-rendered Razor partials with the expected marker are inserted.
            s.content.innerHTML = html; s.status.hidden = true;
        } catch (error) {
            if (error.name === 'AbortError' || s.sequence !== sequence || !s.dialog.open) return;
            s.status.classList.add('is-error');
            s.status.textContent = error instanceof TypeError ? 'Bağlantı kurulamadı. Lütfen tekrar deneyiniz.' : error.message;
        } finally { if (s.sequence === sequence) s.content.setAttribute('aria-busy', 'false'); }
    }
    root.addEventListener('click', event => {
        const trigger = event.target.closest('[data-cs-statement], [data-cs-document]');
        if (!trigger || !root.contains(trigger)) return;
        const s = trigger.hasAttribute('data-cs-document') ? documentDetail : statement;
        if (typeof s.dialog.showModal !== 'function') return; // Native link fallback.
        event.preventDefault(); s.trigger = trigger;
        if (!s.dialog.open) s.dialog.showModal();
        load(s, trigger.href);
    });
    const form = document.getElementById('csFilter');
    function resetScope(resetPeriod) {
        if (resetPeriod) document.getElementById('csPeriod').value = '';
        document.getElementById('csStart').value = ''; document.getElementById('csEnd').value = '';
        // Submit bypasses required validation while the new company's periods are being loaded server-side.
        form.submit();
    }
    document.getElementById('csCompany').addEventListener('change', () => resetScope(true));
    document.getElementById('csPeriod').addEventListener('change', () => resetScope(false));
})();
